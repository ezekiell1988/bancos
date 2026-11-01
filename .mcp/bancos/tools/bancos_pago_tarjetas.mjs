import { existsSync, mkdirSync, readFileSync } from 'node:fs';
import path from 'node:path';
import { contentResult } from '../../_shared/results.mjs';
import { encodeToon } from '../../_shared/toon.mjs';
import { ToolError, checkFecha, getDatosDir, leerMovimientos, r2 } from '../src/datos.mjs';
import { ESTILO, INFODOLAR, cambioInfodolar, crc, esc, fechaCorta, fmt, renderPng, textoDelEstado, ultimoEstado, usd } from '../src/pago.mjs';

const MESES = { ENE: '01', FEB: '02', MAR: '03', ABR: '04', MAY: '05', JUN: '06', JUL: '07', AGO: '08', SET: '09', SEP: '09', OCT: '10', NOV: '11', DIC: '12' };

/** "18-OCT-26" → "2026-10-18". */
function fechaBac(texto) {
  const m = /^(\d{1,2})-([A-Z]{3})-(\d{2})$/.exec(texto ?? '');
  if (!m || !MESES[m[2]]) return null;
  return `20${m[3]}-${MESES[m[2]]}-${m[1].padStart(2, '0')}`;
}

const numero = (s) => Number(s.replace(/,/g, ''));

/** Primera página del estado: una fila CRC y una USD por tarjeta, con pago mínimo y de contado. */
export function leerResumen(texto) {
  const lineas = texto.split('\n');
  const tarjetas = [];
  let actual = null;
  for (const linea of lineas) {
    const crc = /^\s*\*+(\d{4})\s+(.+?)\s{2,}(.+?)\s+CRC\s+\d+\s+([\d,]+\.\d{2})\s+([\d,]+\.\d{2})\s+([\d,]+\.\d{2})\s*$/.exec(linea);
    if (crc) {
      actual = { plastico: crc[1], marca: crc[2].trim(), plan: crc[3].trim(), minimoCRC: numero(crc[5]), contadoCRC: numero(crc[6]), minimoUSD: 0, contadoUSD: 0 };
      tarjetas.push(actual);
      continue;
    }
    const usd = /^\s+USD\s+\d+\s+([\d,]+\.\d{2})\s+([\d,]+\.\d{2})\s+([\d,]+\.\d{2})\s*$/.exec(linea);
    if (usd && actual) {
      actual.minimoUSD = numero(usd[2]);
      actual.contadoUSD = numero(usd[3]);
      actual = null;
    }
  }
  // La cuenta que se paga puede diferir del plástico (Walmart: plástico …8052, cuenta …8058).
  const cuentas = [...texto.matchAll(/Número de cuenta: \*+(\d{4})/g)].map((m) => m[1]);
  const contados = [...texto.matchAll(/PAGO DE CONTADO\s+En Colones\s+([\d,]+\.\d{2})\s+En Dólares\s+([\d,]+\.\d{2})/g)].map((m) => [numero(m[1]), numero(m[2])]);
  for (const t of tarjetas) {
    const i = contados.findIndex(([c, u], k) => c === t.contadoCRC && u === t.contadoUSD && !tarjetas.some((o) => o.cuentaIdx === k));
    t.cuentaIdx = i;
    t.tarjeta = i >= 0 && cuentas[i] ? cuentas[i] : t.plastico;
  }
  const corte = fechaBac(/Fecha de corte:\s+(\S+)/.exec(texto)?.[1]);
  const limite = fechaBac(/Fecha límite pago de contado:\s+(\S+)/.exec(texto)?.[1]);
  return { corte, limite, tarjetas: tarjetas.map(({ cuentaIdx: _i, ...t }) => t) };
}

/** HTML del detalle: tabla por tarjeta, total y monto a pedirle a Karen. */
export function htmlDetalle(r) {
  const filas = r.tarjetas.map((t) => `<tr><td><b>${esc(t.plan)}</b><small>${esc(t.marca)} …${t.tarjeta}</small></td>`
    + `<td>${crc(t.colones)}</td><td>${usd(t.dolares)}</td><td><b>${crc(t.totalColones)}</b></td></tr>`).join('');
  const pagos = r.pagosDesdeCorte.length
    ? r.pagosDesdeCorte.map((p) => `<li>${fechaCorta(p.fecha)}: −${crc(p.total)}</li>`).join('')
    : '<li>Ningún pago a la BAC anotado desde el corte.</li>';
  return `<!doctype html><html><head><meta charset="utf-8"><style>${ESTILO}</style></head><body><div class="c">
<h1>Pago de contado · Tarjetas BAC</h1>
<div class="sub">Estado del corte ${fechaCorta(r.corte)}</div>
<div class="chips"><div class="chip alerta"><span>Fecha límite</span><b>${fechaCorta(r.limite)}</b></div>
<div class="chip"><span>Tipo de cambio (venta)</span><b>₡${fmt(r.tipoCambio.venta)}</b></div>
<div class="chip"><span>Calculado</span><b>${fechaCorta(r.hoy)}</b></div></div>
<table><tr><th>Tarjeta</th><th>Colones</th><th>Dólares</th><th>Total en ₡</th></tr>${filas}
<tr class="t"><td>Total</td><td>${crc(r.total.colones)}</td><td>${usd(r.total.dolares)}</td><td>${crc(r.total.totalColones)}</td></tr></table>
<div class="pedir"><span>Pedirle a Karen (BCR → BAC)</span><b>₡${fmt(r.pedirAKaren, 0)}</b></div>
<ul>${pagos}<li>La BAC usa el tipo de cambio del momento del pago: conviene redondear hacia arriba.</li>
<li>Si los dólares se pagan desde una cuenta en dólares, en colones basta con ${crc(r.total.colones)}.</li></ul>
</div></body></html>`;
}

export default {
  name: 'bancos_pago_tarjetas',
  format: 'toon',
  order: 34,
  description:
    'Cuánto hay que pagar de contado de las tarjetas BAC según un estado de cuenta (PDF): por tarjeta, colones y dólares, ' +
    'pasado todo a colones con la venta del dólar de la BAC de hoy (InfoDolar) o la que se indique. ' +
    'Resta los pagos a la BAC ya anotados en el BCR (tipo tarjeta) desde el corte y da el monto a pedirle a Karen, ' +
    'que administra la cuenta del BCR, para pasarlo a la BAC. Por defecto usa el estado más reciente. ' +
    'Devuelve el detalle como imagen PNG (guardada en datos/reportes/) además del TOON; imagen: false para solo texto.',
  inputSchema: {
    type: 'object',
    properties: {
      estado: { type: 'string', description: 'Ruta del PDF del estado, relativa a datos/ (p. ej. "multimedia/2026-10/2026-10-18-bac-estado-cuenta.pdf"). Default: el más reciente.' },
      tipoCambio: { type: 'number', description: 'Colones por dólar. Default: venta de la BAC hoy según InfoDolar.' },
      hasta: { type: 'string', description: 'AAAA-MM-DD. Hasta qué fecha buscar pagos ya hechos en el BCR. Default: hoy.' },
      imagen: { type: 'boolean', description: 'Generar el detalle como imagen PNG. Default: true.' },
    },
    additionalProperties: false,
  },
  async handler(args) {
    const datosDir = getDatosDir();
    const rel = args.estado ?? ultimoEstado(datosDir, /-bac-estado-cuenta\.(pdf|txt)$/);
    if (!rel) throw new ToolError('No hay ningún estado de la BAC en datos/multimedia. Indicá la ruta en estado.');
    const file = path.resolve(datosDir, rel);
    if (!existsSync(file)) throw new ToolError(`No existe el estado: ${rel}`);
    const { corte, limite, tarjetas } = leerResumen(textoDelEstado(file));
    if (!tarjetas.length) throw new ToolError('No encontré el resumen de tarjetas (PAGO DE CONTADO) en el estado.');

    let cambio;
    if (args.tipoCambio) cambio = { venta: args.tipoCambio, fuente: 'indicado' };
    else {
      cambio = await cambioInfodolar(INFODOLAR.bac);
      if (!cambio) throw new ToolError('No pude leer el tipo de cambio de la BAC. Pasalo en tipoCambio.');
    }

    const filas = tarjetas.map((t) => ({
      tarjeta: t.tarjeta, marca: t.marca, plan: t.plan,
      colones: t.contadoCRC, dolares: t.contadoUSD,
      totalColones: r2(t.contadoCRC + t.contadoUSD * cambio.venta),
      minimoColones: r2(t.minimoCRC + t.minimoUSD * cambio.venta),
    }));
    const suma = (k) => r2(filas.reduce((s, f) => s + f[k], 0));
    const total = suma('totalColones');

    const hasta = args.hasta ?? new Date().toLocaleDateString('en-CA', { timeZone: 'America/Costa_Rica' });
    checkFecha(hasta, 'hasta');
    // Pagos a la BAC desde el BCR después del corte (los «Pago BN» son de otro banco).
    const pagos = corte
      ? leerMovimientos('bcr', corte, hasta).filter((m) => m.tipo === 'tarjeta' && m.fecha > corte && /BAC/i.test(`${m.nota ?? ''} ${m.comercio ?? ''}`))
        .map((m) => ({ fecha: m.fecha, total: m.moneda === 'USD' ? r2(m.total * cambio.venta) : m.total, nota: m.nota ?? '' }))
      : [];
    const pagado = r2(pagos.reduce((s, p) => s + p.total, 0));
    const pedir = Math.max(0, Math.ceil(total - pagado));

    const resultado = {
      estado: rel, corte, limite, hoy: hasta,
      tipoCambio: cambio,
      tarjetas: filas,
      total: { colones: suma('colones'), dolares: suma('dolares'), totalColones: total, minimoColones: suma('minimoColones') },
      pagosDesdeCorte: pagos,
      pedirAKaren: pedir,
      nota: 'Pago de contado = sin intereses. La BAC aplica el tipo de cambio del momento del pago; si los dólares se pagan desde una cuenta en dólares, en colones basta con total.colones.',
    };
    if (args.imagen === false) return resultado;

    const dir = path.join(datosDir, 'reportes');
    mkdirSync(dir, { recursive: true });
    const png = path.join(dir, `pago-tarjetas-${corte ?? 'bac'}-${hasta}.png`);
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
    writeFileSync(path.join(dir, '2026-10-18-bac-estado-cuenta.txt'), [
      '************1593                                       VISA              PRICECASH         CRC                   0             0.00         8,379.00            61,150.60',
      '                                                                                           USD                   0             0.00             0.00                 0.00',
      '************8608                            MASTER CARD             MILLAS PLUS CRED. MC   CRC                   0             0.00        22,686.00           396,180.76',
      '                                                                                           USD                   0             0.00           238.00               440.19',
      'Fecha de corte: 18-OCT-26',
      'Fecha límite pago de contado: 2-NOV-26',
      'Número de cuenta: ************1593',
      '     2-NOV-26         PAGO DE CONTADO                               En Colones                  61,150.60                     En Dólares                        0.00',
      'Número de cuenta: ************8608',
      '     2-NOV-26         PAGO DE CONTADO                               En Colones                396,180.76                      En Dólares                      440.19',
    ].join('\n'));
    const text = toolText(await callTool('bancos_pago_tarjetas', { tipoCambio: 500, hasta: '2026-10-31', imagen: false }));
    check('bancos_pago_tarjetas suma colones y dólares al cambio', text.includes('677426.36') && /pedirAKaren: 677427/.test(text) && text.includes('2026-11-02'), text.slice(0, 500));
  },
};
