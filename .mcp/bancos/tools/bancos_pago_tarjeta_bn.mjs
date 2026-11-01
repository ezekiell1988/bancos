import { existsSync, mkdirSync, readFileSync } from 'node:fs';
import path from 'node:path';
import { contentResult } from '../../_shared/results.mjs';
import { encodeToon } from '../../_shared/toon.mjs';
import { ToolError, checkFecha, getDatosDir, leerMovimientos, r2 } from '../src/datos.mjs';
import { ESTILO, INFODOLAR, cambioInfodolar, crc, esc, fechaCorta, fmt, renderPng, textoDelEstado, ultimoEstado, usd } from '../src/pago.mjs';

/** "19/10/2026" → "2026-10-19". */
const fechaBn = (texto) => { const m = /^(\d{2})\/(\d{2})\/(\d{4})$/.exec(texto ?? ''); return m ? `${m[3]}-${m[2]}-${m[1]}` : null; };
const numero = (s) => Number(s.replace(/,/g, ''));
const campo = (texto, etiqueta) => new RegExp(`^\\s*${etiqueta}\\s+(\\S.*?)(?:\\s{2,}|$)`, 'm').exec(texto)?.[1].trim() ?? null;

/** Primera página del estado del BN: una tarjeta, con pago mínimo y de contado en colones y dólares. */
export function leerEstadoBn(texto) {
  const par = (re) => { const m = re.exec(texto); return m ? [numero(m[1]), numero(m[2])] : null; };
  const contado = par(/TOTAL PAGO DE CONTADO\*?\s+([\d,]+\.\d{2})\s+([\d,]+\.\d{2})/)
    ?? par(/PAGO DE CONTADO:\s+En colones:\s+([\d,]+\.\d{2})\s+En dólares:\s+([\d,]+\.\d{2})/);
  const minimo = par(/TOTAL PAGO MÍNIMO\*?\s+([\d,]+\.\d{2})\s+([\d,]+\.\d{2})/) ?? [0, 0];
  return {
    tarjeta: /Número de cuenta\s+\*+(\d{4})/.exec(texto)?.[1] ?? null,
    marca: campo(texto, 'Marca de la tarjeta'),
    dueno: campo(texto, 'Dueño de la cuenta'),
    corte: fechaBn(campo(texto, 'Fecha de emisión y corte')),
    limite: fechaBn(campo(texto, 'Fecha límite de pago de contado')),
    contado, minimo,
  };
}

/** HTML del detalle: la tarjeta, total en colones y lo que falta pagar. */
export function htmlDetalle(r) {
  const t = r.tarjeta;
  const pagos = r.pagosDesdeCorte.length
    ? r.pagosDesdeCorte.map((p) => `<li>${fechaCorta(p.fecha)}: −${crc(p.total)} (BCR, ${esc(p.nota || 'Pago BN')})</li>`).join('')
    : '<li>Ningún pago al BN anotado en el BCR desde el corte.</li>';
  return `<!doctype html><html><head><meta charset="utf-8"><style>${ESTILO}</style></head><body><div class="c">
<h1>Pago de contado · Tarjeta BN</h1>
<div class="sub">Estado del corte ${fechaCorta(r.corte)}${t.dueno ? ` · ${esc(t.dueno)}` : ''}</div>
<div class="chips"><div class="chip alerta"><span>Fecha límite</span><b>${fechaCorta(r.limite)}</b></div>
<div class="chip"><span>Tipo de cambio BN (venta)</span><b>₡${fmt(r.tipoCambio.venta)}</b></div>
<div class="chip"><span>Calculado</span><b>${fechaCorta(r.hoy)}</b></div></div>
<table><tr><th>Tarjeta</th><th>Colones</th><th>Dólares</th><th>Total en ₡</th></tr>
<tr><td><b>Banco Nacional</b><small>${esc(t.marca ?? '')} …${t.tarjeta}</small></td><td>${crc(t.colones)}</td><td>${usd(t.dolares)}</td><td><b>${crc(t.totalColones)}</b></td></tr>
<tr class="t"><td>Pago mínimo</td><td>${crc(t.minimoColonesSolo)}</td><td>${usd(t.minimoDolares)}</td><td>${crc(t.minimoColones)}</td></tr></table>
<div class="pedir"><span>Falta pagar de contado</span><b>₡${fmt(r.faltaPagar, 0)}</b></div>
<ul>${pagos}<li>El BN usa el tipo de cambio del momento del pago: conviene redondear hacia arriba.</li>
<li>Si los dólares se pagan desde una cuenta en dólares, en colones basta con ${crc(t.colones)}.</li></ul>
</div></body></html>`;
}

export default {
  name: 'bancos_pago_tarjeta_bn',
  format: 'toon',
  order: 35,
  description:
    'Cuánto hay que pagar de contado de la tarjeta del Banco Nacional (BNCR, la de Karen …2921) según su estado de cuenta (PDF): ' +
    'colones y dólares, pasado todo a colones con la venta del dólar del BN de hoy (InfoDolar) o la que se indique. ' +
    'Resta los «Pago BN» ya anotados en el BCR (tipo tarjeta) desde el corte y da faltaPagar. Por defecto usa el estado más reciente. ' +
    'Devuelve el detalle como imagen PNG (guardada en datos/reportes/) además del TOON; imagen: false para solo texto.',
  inputSchema: {
    type: 'object',
    properties: {
      estado: { type: 'string', description: 'Ruta del PDF del estado, relativa a datos/ (p. ej. "multimedia/2026-10/2026-10-19-estado-bn-2921.pdf"). Default: el más reciente.' },
      tipoCambio: { type: 'number', description: 'Colones por dólar. Default: venta del BN hoy según InfoDolar.' },
      hasta: { type: 'string', description: 'AAAA-MM-DD. Hasta qué fecha buscar pagos ya hechos en el BCR. Default: hoy.' },
      imagen: { type: 'boolean', description: 'Generar el detalle como imagen PNG. Default: true.' },
    },
    additionalProperties: false,
  },
  async handler(args) {
    const datosDir = getDatosDir();
    const rel = args.estado ?? ultimoEstado(datosDir, /-estado-bn-\d{4}\.(pdf|txt)$/);
    if (!rel) throw new ToolError('No hay ningún estado del BN en datos/multimedia (…-estado-bn-NNNN.pdf). Indicá la ruta en estado.');
    const file = path.resolve(datosDir, rel);
    if (!existsSync(file)) throw new ToolError(`No existe el estado: ${rel}`);
    const e = leerEstadoBn(textoDelEstado(file));
    if (!e.contado) throw new ToolError('No encontré el PAGO DE CONTADO en el estado del BN.');

    let cambio;
    if (args.tipoCambio) cambio = { venta: args.tipoCambio, fuente: 'indicado' };
    else {
      cambio = await cambioInfodolar(INFODOLAR.bncr);
      if (!cambio) throw new ToolError('No pude leer el tipo de cambio del BN. Pasalo en tipoCambio.');
    }

    const [colones, dolares] = e.contado;
    const tarjeta = {
      tarjeta: e.tarjeta, marca: e.marca, dueno: e.dueno,
      colones, dolares, totalColones: r2(colones + dolares * cambio.venta),
      minimoColonesSolo: e.minimo[0], minimoDolares: e.minimo[1], minimoColones: r2(e.minimo[0] + e.minimo[1] * cambio.venta),
    };

    const hasta = args.hasta ?? new Date().toLocaleDateString('en-CA', { timeZone: 'America/Costa_Rica' });
    checkFecha(hasta, 'hasta');
    // Lo que sale del BCR para el BN se anota como «Pago BN» (tipo tarjeta).
    const pagos = e.corte
      ? leerMovimientos('bcr', e.corte, hasta).filter((m) => m.tipo === 'tarjeta' && m.fecha > e.corte && /\bBN\b|Nacional/i.test(`${m.nota ?? ''} ${m.comercio ?? ''}`))
        .map((m) => ({ fecha: m.fecha, total: m.moneda === 'USD' ? r2(m.total * cambio.venta) : m.total, nota: m.nota ?? '' }))
      : [];
    const pagado = r2(pagos.reduce((s, p) => s + p.total, 0));

    const resultado = {
      estado: rel, corte: e.corte, limite: e.limite, hoy: hasta,
      tipoCambio: cambio,
      tarjeta,
      pagosDesdeCorte: pagos,
      faltaPagar: Math.max(0, Math.ceil(tarjeta.totalColones - pagado)),
      nota: 'Pago de contado = sin intereses. El BN aplica el tipo de cambio del momento del pago; si los dólares se pagan desde una cuenta en dólares, en colones basta con tarjeta.colones.',
    };
    if (args.imagen === false) return resultado;

    const dir = path.join(datosDir, 'reportes');
    mkdirSync(dir, { recursive: true });
    const png = path.join(dir, `pago-tarjeta-bn-${e.corte ?? 'bn'}-${hasta}.png`);
    renderPng(htmlDetalle(resultado), png);
    resultado.imagen = png;
    return contentResult([
      { type: 'text', text: encodeToon(resultado) },
      { type: 'image', data: readFileSync(png).toString('base64'), mimeType: 'image/png' },
    ]);
  },
  async smoke({ callTool, check, toolText, state }) {
    const { mkdirSync, writeFileSync } = await import('node:fs');
    const dir = path.join(state.datosDir, 'multimedia', '2026-10');
    mkdirSync(dir, { recursive: true });
    writeFileSync(path.join(dir, '2026-10-19-estado-bn-2921.txt'), [
      '  Marca de la tarjeta                           MASTER CARD',
      '  Número de cuenta                              ************2921                                ESTIMADO TARJETAHABIENTE',
      '  Dueño de la cuenta                            KAREN SOTO A.',
      '  Fecha de emisión y corte                      19/10/2026                                      OCTUBRE DE 2026',
      '  Fecha límite de pago de contado               03/11/2026                                      ACLARAR CONSULTAS',
      'TOTAL PAGO MÍNIMO*                                  5,000.00                   6.99 TOTAL PAGO DE CONTADO*                                        138,276.10                    6.99',
    ].join('\n'));
    const text = toolText(await callTool('bancos_pago_tarjeta_bn', { tipoCambio: 500, hasta: '2026-10-31', imagen: false }));
    check('bancos_pago_tarjeta_bn suma colones y dólares al cambio', text.includes('141771.1') && /faltaPagar: 141772/.test(text) && text.includes('2026-11-03'), text.slice(0, 500));
  },
};
