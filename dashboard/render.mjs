// HTML del dashboard y de los reportes. Una sola vista sirve a la pantalla y a la imagen (img=1: ancho fijo 1080 px, fuente grande).
// Los números salen de los mismos handlers que los tools bancos_* del MCP, así cuadran con lo que ve el agente.
import { existsSync } from 'node:fs';
import path from 'node:path';
import movimientos from '../.mcp/bancos/tools/bancos_movimientos.mjs';
import pagoTarjetas from '../.mcp/bancos/tools/bancos_pago_tarjetas.mjs';
import pendientes from '../.mcp/bancos/tools/bancos_pendientes.mjs';
import resumen from '../.mcp/bancos/tools/bancos_resumen.mjs';
import { CUENTAS, HOGAR, getDatosDir, leerTipos } from '../.mcp/bancos/src/datos.mjs';
import { consultar } from '../.mcp/bancos/src/sqlite.mjs';
import { cicloDe, rangoCiclo } from '../.mcp/bancos/src/periodo.mjs';
import { esc, fechaCorta, fmt } from '../.mcp/bancos/src/pago.mjs';

export const REPORTES = {
  resumen: 'Resumen del periodo',
  tipos: 'Gastos por tipo',
  pago: 'Pago de contado de las tarjetas',
  pendientes: 'Conciliación',
  movimientos: 'Movimientos',
};
const MAX_FILAS = { pantalla: 300, imagen: 120 };

const hoyCR = () => new Date().toLocaleDateString('en-CA', { timeZone: 'America/Costa_Rica' });
const sumarCiclo = (ciclo, n) => {
  const [a, m] = ciclo.split('-').map(Number);
  const i = a * 12 + (m - 1) + n;
  return `${Math.floor(i / 12)}-${String((i % 12) + 1).padStart(2, '0')}`;
};

/** Periodo pedido en la URL → { args para los handlers, etiqueta, modo }. Por defecto, el ciclo actual. */
export function periodoDe(q) {
  const cuenta = CUENTAS.includes(q.get('cuenta')) ? { cuenta: q.get('cuenta') } : {};
  const fecha = /^\d{4}-\d{2}-\d{2}$/;
  if (fecha.test(q.get('desde') ?? '') || fecha.test(q.get('hasta') ?? '')) {
    const d = fecha.test(q.get('desde') ?? '') ? q.get('desde') : undefined;
    const h = fecha.test(q.get('hasta') ?? '') ? q.get('hasta') : undefined;
    return { modo: 'rango', args: { desde: d, hasta: h, ...cuenta }, cuenta: cuenta.cuenta, ciclo: cicloDe(h ?? d ?? hoyCR()), mes: (h ?? d ?? hoyCR()).slice(0, 7), desde: d, hasta: h };
  }
  if (/^\d{4}-(0[1-9]|1[0-2])$/.test(q.get('mes') ?? '')) {
    const mes = q.get('mes');
    return { modo: 'mes', args: { mes, ...cuenta }, cuenta: cuenta.cuenta, ciclo: cicloDe(`${mes}-01`), mes };
  }
  const ciclo = /^\d{4}-(0[1-9]|1[0-2])$/.test(q.get('ciclo') ?? '') ? q.get('ciclo') : sumarCiclo(cicloDe(hoyCR()), -1);
  return { modo: 'ciclo', args: { ciclo, ...cuenta }, cuenta: cuenta.cuenta, ciclo, mes: ciclo };
}

/** Query string que reproduce el periodo (para enlaces y para el reporte que Chrome abre). */
export function queryDe(p, extra = {}) {
  const q = new URLSearchParams();
  if (p.modo === 'ciclo') q.set('ciclo', p.ciclo);
  else if (p.modo === 'mes') q.set('mes', p.mes);
  else { if (p.desde) q.set('desde', p.desde); if (p.hasta) q.set('hasta', p.hasta); }
  if (p.cuenta) q.set('cuenta', p.cuenta);
  for (const [k, v] of Object.entries(extra)) q.set(k, v);
  return q.toString();
}

const money = (n, moneda) => `${n < 0 ? '−' : ''}${moneda === 'USD' ? '$' : '₡'}${fmt(Math.abs(n), 2)}`;
// En pantalla los colones van sin céntimos para que quepan en el iPhone; la imagen (1080 px) los conserva.
const moneyCorto = (n, moneda) => (moneda === 'USD' ? money(n, moneda) : `${n < 0 ? '−' : ''}₡${fmt(Math.abs(n), 0)}`);
const rubroNombre = (r) => (r === HOGAR ? 'Hogar' : r === 'papa' ? 'Papá' : r.charAt(0).toUpperCase() + r.slice(1));
const nombresTipo = () => new Map([...leerTipos('gasto'), ...leerTipos('ingreso')].map((t) => [t.id, t.nombre]));

const FILTROS = ['estado', 'clase', 'rubro', 'moneda', 'tipo', 'tarjeta', 'desde', 'hasta'];
/** Filtro del detalle desde la URL (claves f_*), solo valores sencillos. */
export function filtroDe(q) {
  const f = {};
  for (const k of FILTROS) { const v = q.get(`f_${k}`); if (v && /^[\w.-]{1,40}$/.test(v)) f[k] = v; }
  return f;
}
const queryFiltro = (f) => Object.fromEntries(Object.entries(f).map(([k, v]) => [`f_${k}`, v]));
/** Envuelve un monto en un enlace a su detalle (en la imagen va como texto). */
const liga = (d, f, html) => (d.imagen ? html : `<a class="mt" href="detalle.html?${queryDe(d.p, queryFiltro(f))}">${html}</a>`);
const fechaMas = (f, dias) => { const x = new Date(`${f}T12:00:00Z`); x.setUTCDate(x.getUTCDate() + dias); return x.toISOString().slice(0, 10); };
const mesAntes = (f) => { const x = new Date(`${f}T12:00:00Z`); x.setUTCMonth(x.getUTCMonth() - 1); return x.toISOString().slice(0, 10); };
const diaDespues = (f) => fechaMas(f, 1);
const rango = (r) => `${fechaCorta(r.desde)} al ${fechaCorta(r.hasta)}`;

function tabla(cabeza, filas, { total = null, clase = '' } = {}) {
  return `<div class="tw"><table${cabeza.length > 3 ? ` class="ap ${clase}"` : ''}><thead><tr>${cabeza.map((c, i) => `<th${i ? ' class="n"' : ''}>${esc(c)}</th>`).join('')}</tr></thead><tbody>`
    + filas.map((f) => `<tr>${f.map((c, i) => `<td${i ? ` class="n" data-l="${esc(cabeza[i])}"` : ''}>${c}</td>`).join('')}</tr>`).join('')
    + (total ? `<tr class="t">${total.map((c, i) => `<td${i ? ` class="n" data-l="${esc(cabeza[i])}"` : ''}>${c}</td>`).join('')}</tr>` : '')
    + '</tbody></table></div>';
}

/** Datos de un periodo; cada parte falla sola para que una caída (p. ej. el tipo de cambio) no tumbe la pantalla. */
export async function cargar(p, { imagen = false } = {}) {
  const intentar = async (fn) => { try { return { ok: await fn() }; } catch (e) { return { error: e.message }; } };
  const [r, m, pe, pago] = await Promise.all([
    intentar(() => resumen.handler(p.args)),
    intentar(() => movimientos.handler(p.args)),
    intentar(() => pendientes.handler(p.args)),
    intentar(() => pagoTarjetas.handler({ imagen: false })),
  ]);
  return { p, r, m, pe, pago, imagen, max: imagen ? MAX_FILAS.imagen : MAX_FILAS.pantalla, money: imagen ? money : moneyCorto };
}

const aviso = (e) => `<p class="vacio">No se pudo cargar: ${esc(e)}</p>`;

const secciones = {
  resumen(d) {
    if (d.r.error) return aviso(d.r.error);
    const r = d.r.ok;
    const bal = r.balance.map((b) => `<div class="chips"><div class="chip"><span>Ingresos ${b.moneda}</span><b>${liga(d, { moneda: b.moneda, clase: 'ingreso' }, d.money(b.ingresos, b.moneda))}</b></div>`
      + `<div class="chip"><span>Gastos ${b.moneda}</span><b>${liga(d, { moneda: b.moneda, clase: 'gasto' }, d.money(b.gastos, b.moneda))}</b></div>`
      + `<div class="chip ${b.balance < 0 ? 'alerta' : 'bien'}"><span>Balance ${b.moneda}</span><b>${liga(d, { moneda: b.moneda }, d.money(b.balance, b.moneda))}</b></div></div>`).join('');
    const rubros = r.rubros.length ? tabla(['Rubro', 'Ingresos', 'Gastos', 'Balance'],
      r.rubros.map((x) => [`${esc(rubroNombre(x.rubro))} <small>${x.moneda} · ${x.movimientos} mov.</small>`, liga(d, { rubro: x.rubro, moneda: x.moneda, clase: 'ingreso' }, d.money(x.ingresos, x.moneda)), liga(d, { rubro: x.rubro, moneda: x.moneda, clase: 'gasto' }, d.money(x.gastos, x.moneda)), `<b>${liga(d, { rubro: x.rubro, moneda: x.moneda }, d.money(x.balance, x.moneda))}</b>`])) : '';
    return `${bal || '<p class="vacio">Sin movimientos en este periodo.</p>'}${rubros}`;
  },
  tipos(d) {
    if (d.r.error) return aviso(d.r.error);
    const nombres = nombresTipo();
    const gastos = d.r.ok.tipos.filter((t) => t.clase === 'gasto');
    if (!gastos.length) return '<p class="vacio">Sin gastos en este periodo.</p>';
    const grupos = Object.groupBy(gastos, (t) => `${t.rubro}|${t.moneda}`);
    return Object.entries(grupos).map(([k, lista]) => {
      const [rubro, moneda] = k.split('|');
      const suma = lista.reduce((s, t) => s + t.total, 0);
      return `<h3>${esc(rubroNombre(rubro))} · ${moneda}</h3>${tabla(['Tipo', 'Cant.', 'Total'],
        lista.map((t) => [esc(nombres.get(t.tipo) ?? t.tipo), liga(d, { rubro, moneda, clase: 'gasto', tipo: t.tipo }, String(t.cantidad)), liga(d, { rubro, moneda, clase: 'gasto', tipo: t.tipo }, d.money(t.total, moneda))]), { total: ['Total', String(lista.reduce((s, t) => s + t.cantidad, 0)), liga(d, { rubro, moneda, clase: 'gasto' }, d.money(suma, moneda))] })}`;
    }).join('');
  },
  pago(d) {
    if (d.pago.error) return aviso(d.pago.error);
    const g = d.pago.ok;
    const ini = diaDespues(mesAntes(g.corte));
    const ft = (t, moneda) => ({ tarjeta: t.tarjeta, desde: ini, hasta: g.corte, ...(moneda ? { moneda } : {}), estado: String(moneda === 'USD' ? t.dolares : moneda === 'CRC' ? t.colones : t.totalColones) });
    const filas = g.tarjetas.map((t) => [`<b>${esc(t.plan)}</b><small>${esc(t.marca)} …${esc(t.tarjeta)}</small>`, liga(d, ft(t, 'CRC'), d.money(t.colones, 'CRC')), liga(d, ft(t, 'USD'), d.money(t.dolares, 'USD')), `<b>${liga(d, ft(t), d.money(t.totalColones, 'CRC'))}</b>`]);
    return `<p class="sub">Estado del corte ${fechaCorta(g.corte)} · límite ${fechaCorta(g.limite)} · dólar ${d.money(g.tipoCambio.venta, 'CRC')}</p>`
      + tabla(['Tarjeta', 'Colones', 'Dólares', 'Total en ₡'], filas, { total: ['Total', d.money(g.total.colones, 'CRC'), d.money(g.total.dolares, 'USD'), d.money(g.total.totalColones, 'CRC')] })
      + `<div class="pedir"><span>Falta por pagar${g.pagosDesdeCorte.length ? ` (ya se pagó ${d.money(g.pagosDesdeCorte.reduce((s, x) => s + x.total, 0), 'CRC')})` : ''}</span><b>${d.money(g.pedirAKaren, 'CRC')}</b></div>`;
  },
  pendientes(d) {
    if (d.pe.error) return aviso(d.pe.error);
    const pe = d.pe.ok;
    const sinCat = pe.sinCategoria?.lista ?? [];
    const sinMov = pe.libroSinMovimiento?.lista ?? [];
    const saldo = pe.noCuadran?.lista ?? [];
    const lista = (titulo, filas, ayuda) => (filas.length ? `<h3>${titulo} (${filas.length})</h3><p class="sub">${ayuda}</p><div class="lista">${filas.slice(0, d.max).map((f) => f).join('')}${filas.length > d.max ? `<p class="vacio">y ${filas.length - d.max} más</p>` : ''}</div>` : '');
    const fila = (fecha, texto, sub, monto) => `<div class="f"><div class="d"><span class="fe">${fechaCorta(fecha)}</span> ${esc(texto)}${sub ? `<small>${esc(sub)}</small>` : ''}</div><div class="m">${monto}</div></div>`;
    const html = lista('Documentos sin movimiento', sinMov.map((x) => fila(x.fecha, x.comercio || '—', `Pago${x.referencia ? ` ${x.referencia}` : ''}`, d.money(x.total, x.moneda))), 'Esperan el estado de cuenta, o su línea no se encontró: se vinculan al movimiento.')
      + lista('Sumas que no cuadran', saldo.map((x) => fila(x.fecha, x.comercio, x.problemas, d.money(x.total, x.moneda))), 'Las facturas o partidas no dan igual al pago.')
      + lista('Sin categoría', sinCat.map((x) => fila(x.fecha, x.comercio || x.tipo || '—', `${x.cuenta.toUpperCase()} · ${x.clase}`, d.money(x.total, x.moneda))), 'Quedaron en «otros»: falta ponerles un tipo.');
    return html || '<p class="vacio">Todo conciliado en este periodo ✅</p>';
  },
  movimientos(d) {
    if (d.m.error) return aviso(d.m.error);
    const nombres = nombresTipo();
    const todos = d.m.ok.movimientos;
    if (!todos.length) return '<p class="vacio">Sin movimientos.</p>';
    const filas = todos.slice(0, d.max).map((x) => {
      const signo = x.clase === 'ingreso' ? '' : '−';
      const sub = [x.cuenta.toUpperCase(), nombres.get(x.tipo) ?? x.tipo, x.rubro !== HOGAR ? rubroNombre(x.rubro) : ''].filter(Boolean).join(' · ');
      return `<div class="f"><div class="d"><span class="fe">${fechaCorta(x.fecha)}</span> ${esc(x.comercio || x.nota || '—')}<small>${esc(sub)}</small></div>`
        + `<div class="m ${x.clase}">${signo}${d.money(x.total, x.moneda).replace('−', '')}</div></div>`;
    });
    return `<div class="lista">${filas.join('')}</div>${todos.length > d.max ? `<p class="vacio">Se muestran ${d.max} de ${todos.length}.</p>` : ''}`;
  },
};

const ESTILO = `
:root{--fg:#1d2433;--mu:#5b6475;--bg:#f4f6fa;--card:#fff;--li:#e3e7ee;--ac:#2563eb;--ok:#15803d;--mal:#c2410c;--ch:#eef1f6}
@media(prefers-color-scheme:dark){:root:not(.img){--fg:#e7eaf1;--mu:#9aa3b5;--bg:#12151c;--card:#1b2029;--li:#2a303c;--ac:#6ea0ff;--ok:#4ade80;--mal:#fb923c;--ch:#232936}}
*{box-sizing:border-box}html{-webkit-text-size-adjust:100%}
body{margin:0;font:15px/1.4 -apple-system,'Helvetica Neue',sans-serif;color:var(--fg);background:var(--bg);overflow-wrap:break-word}
.w{padding:12px 16px 40px;max-width:900px;margin:0 auto}
h1{font-size:20px;margin:6px 0 2px}h2{font-size:17px;margin:0}h3{font-size:14px;margin:16px 0 6px;color:var(--mu)}
.sub{color:var(--mu);margin:2px 0 12px;font-size:13px}
.card{background:var(--card);border:1px solid var(--li);border-radius:14px;padding:14px;margin:12px 0}
.cab{display:flex;justify-content:space-between;align-items:center;gap:10px;margin-bottom:10px}
.btn{font:600 13px inherit;font-family:inherit;border:0;border-radius:10px;background:var(--ac);color:#fff;padding:9px 12px;cursor:pointer;white-space:nowrap}
.btn:disabled{opacity:.5}
.bar{display:flex;flex-wrap:wrap;gap:6px;margin:8px 0}
.bar a,.bar button{font:600 13px inherit;font-family:inherit;text-decoration:none;color:var(--fg);background:var(--ch);border:1px solid var(--li);border-radius:999px;padding:7px 12px}
.bar a.on{background:var(--ac);color:#fff;border-color:var(--ac)}
form.rg{display:flex;flex-wrap:wrap;gap:8px;align-items:center;margin:6px 0}
form.rg input,form.rg select{font:14px inherit;font-family:inherit;padding:7px;border:1px solid var(--li);border-radius:8px;background:var(--card);color:var(--fg);min-width:0}
.chips{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:8px;margin-bottom:10px}
.chip{background:var(--ch);border-radius:12px;padding:8px 10px;min-width:0}.chip span{display:block;font-size:12px;color:var(--mu)}.chip b{font-size:15px}
.chip.alerta b{color:var(--mal)}.chip.bien b{color:var(--ok)}
.tw{width:100%}table{width:100%;border-collapse:collapse;table-layout:auto}
th{font-size:12px;color:var(--mu);text-align:left;padding:6px 4px;border-bottom:2px solid var(--li)}
td{padding:8px 4px;border-bottom:1px solid var(--li);vertical-align:top}
th.n,td.n{text-align:right;white-space:nowrap}
td small,.f small{display:block;color:var(--mu);font-size:12px}
tr.t td{border-top:2px solid var(--fg);border-bottom:0;font-weight:700}
.lista .f{display:grid;grid-template-columns:minmax(0,1fr) auto;gap:10px;padding:9px 0;border-bottom:1px solid var(--li)}
.fe{color:var(--mu);font-size:12px;margin-right:4px}.m{text-align:right;white-space:nowrap;font-weight:600}.m.ingreso{color:var(--ok)}
.pedir{margin-top:12px;background:#e8f5ec;border-radius:12px;padding:12px 14px;display:flex;justify-content:space-between;gap:10px;align-items:center;color:#1f6b3a}
.pedir b{font-size:20px;color:#15803d;white-space:nowrap}
.mt{color:inherit;text-decoration:underline dotted;text-underline-offset:3px;text-decoration-thickness:1px}
.btn.sec{background:var(--ch);color:var(--fg);border:1px solid var(--li)}
.acc{display:flex;gap:8px;margin:8px 0}.acc .btn{flex:1;padding:11px 12px;font-size:15px}
.vacio{color:var(--mu);margin:8px 0}
.tabs{display:flex;gap:6px;margin:12px 0 0;padding:4px;background:var(--ch);border-radius:12px}
.tabs button{flex:1;font:600 14px inherit;font-family:inherit;border:0;border-radius:9px;background:none;color:var(--mu);padding:9px 8px;cursor:pointer}
.tabs button.on{background:var(--card);color:var(--fg);box-shadow:0 1px 3px rgba(0,0,0,.12)}
.tabs .cnt{display:inline-block;margin-left:6px;min-width:20px;padding:1px 6px;border-radius:999px;background:var(--mal);color:#fff;font-size:12px}
.tabs .cnt.cero{background:var(--ok)}
.panel[hidden]{display:none}
#aviso{position:fixed;left:16px;right:16px;bottom:16px;background:var(--fg);color:var(--bg);padding:12px 14px;border-radius:12px;display:none;text-align:center}
@media(max-width:560px){
html:not(.img) .chips{grid-template-columns:1fr;gap:4px}
html:not(.img) .chip{display:flex;justify-content:space-between;align-items:baseline;gap:10px;padding:8px 12px}
html:not(.img) .chip span{display:inline;font-size:13px}html:not(.img) .chip b{font-size:16px;white-space:nowrap}
html:not(.img) table.ap thead{display:none}
html:not(.img) table.ap tr{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:4px 8px;padding:10px 0;border-bottom:1px solid var(--li)}
html:not(.img) table.ap td{border:0;padding:0;text-align:left;white-space:nowrap}
html:not(.img) table.ap td:first-child{grid-column:1/-1;white-space:normal;font-weight:600}
html:not(.img) table.ap td:first-child small{display:inline;margin-left:6px;font-weight:400}
html:not(.img) table.ap td.n::before{content:attr(data-l);display:block;font-size:11px;color:var(--mu);font-weight:400}
html:not(.img) table.ap tr.t{border-top:2px solid var(--fg);border-bottom:0}
html:not(.img) .cab h2{font-size:16px}
html:not(.img) table.det tr{grid-template-columns:auto minmax(0,1fr) auto}
html:not(.img) table.det td{white-space:normal}
html:not(.img) table.det td.n:last-child{text-align:right;white-space:nowrap}
html:not(.img) table.ap td.n:empty{display:none}
html:not(.img) table.ap tr.t td:last-child{grid-column:1/-1;display:flex;justify-content:space-between}
html:not(.img) table.ap tr.t td.n::before{display:inline}
}
html.img{font-size:26px}html.img body{font-size:26px;width:1080px;background:#fff}
html.img .w{max-width:none;padding:32px 36px}html.img h1{font-size:40px}html.img h2{font-size:32px}html.img h3{font-size:25px}
html.img .sub,html.img td small,html.img .f small,html.img .fe,html.img th,html.img .chip span{font-size:21px}
html.img .chip b{font-size:28px}html.img .card{border:0;padding:0;margin:20px 0;background:#fff}html.img .pedir b{font-size:40px}
html.img td{padding:14px 8px}html.img .lista .f{padding:14px 0}html.img .m{font-size:26px}
`;

const pagina = (titulo, cuerpo, { imagen = false, extra = '' } = {}) => `<!doctype html><html lang="es"${imagen ? ' class="img"' : ''}><head><meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1"><title>${esc(titulo)}</title><style>${ESTILO}</style></head><body>${cuerpo}${extra}</body></html>`;

/** Reporte suelto para la imagen: ancho fijo 1080; el script deja su alto en <html data-alto> para que Chrome lo mida. */
export async function htmlReporte(p, id, { imagen = true } = {}) {
  if (!REPORTES[id]) throw new Error(`Reporte desconocido: ${id}`);
  const d = await cargar(p, { imagen });
  const r = d.r.ok ?? d.m.ok;
  const etiqueta = r ? rango(r) : '';
  const cuenta = p.cuenta ? ` · ${p.cuenta.toUpperCase()}` : '';
  const cuerpo = `<div class="w"><h1>${esc(REPORTES[id])}</h1><div class="sub">${etiqueta}${cuenta}</div>${secciones[id](d)}</div>`;
  return pagina(REPORTES[id], cuerpo, { imagen, extra: imagen ? '<script>document.documentElement.dataset.alto=document.documentElement.scrollHeight</script>' : '' });
}

/** Pantalla principal: selector de periodo y todas las secciones, cada una con su botón «Enviar al chat». */
export async function htmlIndex(p) {
  const d = await cargar(p);
  const r = d.r.ok ?? d.m.ok;
  const q = (extra) => `?${queryDe(p, extra)}`;
  const enlace = (texto, params, on) => `<a href="?${new URLSearchParams(params)}${p.cuenta && !params.cuenta ? `&cuenta=${p.cuenta}` : ''}"${on ? ' class="on"' : ''}>${texto}</a>`;
  const ciclo = p.ciclo;
  const selector = `<div class="bar">${enlace('Ciclo', { ciclo }, p.modo === 'ciclo')}${enlace('Mes', { mes: p.mes }, p.modo === 'mes')}${enlace('Rango', { desde: p.desde ?? rangoCiclo(ciclo).desde, hasta: p.hasta ?? rangoCiclo(ciclo).hasta }, p.modo === 'rango')}</div>`
    + (p.modo === 'ciclo' ? `<div class="bar">${enlace('◀', { ciclo: sumarCiclo(ciclo, -1) })}${enlace('Actual', { ciclo: cicloDe(hoyCR()) }, ciclo === cicloDe(hoyCR()))}${enlace('▶', { ciclo: sumarCiclo(ciclo, 1) })}</div>` : '')
    + (p.modo === 'mes' ? `<div class="bar">${enlace('◀', { mes: sumarCiclo(p.mes, -1) })}${enlace('Hoy', { mes: hoyCR().slice(0, 7) }, p.mes === hoyCR().slice(0, 7))}${enlace('▶', { mes: sumarCiclo(p.mes, 1) })}</div>` : '')
    + (p.modo === 'rango' ? `<form class="rg"><input type="date" name="desde" value="${p.desde ?? ''}"><input type="date" name="hasta" value="${p.hasta ?? ''}">${p.cuenta ? `<input type="hidden" name="cuenta" value="${p.cuenta}">` : ''}<button class="btn">Ver</button></form>` : '')
    + `<div class="bar"><a href="?${queryDe({ ...p, cuenta: undefined })}"${p.cuenta ? '' : ' class="on"'}>Todas</a>${CUENTAS.map((c) => `<a href="?${queryDe({ ...p, cuenta: c })}"${p.cuenta === c ? ' class="on"' : ''}>${c.toUpperCase()}</a>`).join('')}</div>`;
  const tarjeta = (id) => `<section class="card"><div class="cab"><h2>${esc(REPORTES[id])}</h2><button class="btn" data-r="${id}">Enviar al chat</button></div>${secciones[id](d)}</section>`;
  // Conciliación va en su propia pestaña, con el número de pendientes para llevarlo a 0.
  const pe = d.pe.ok;
  const faltan = pe ? (pe.libroSinMovimiento?.total ?? 0) + (pe.noCuadran?.total ?? 0) + (pe.sinCategoria?.total ?? 0) : null;
  const general = Object.keys(REPORTES).filter((id) => id !== 'pendientes');
  const categorias = `<section class="card"><div class="cab"><h2>Por categoría</h2></div>${seccionCategorias(p)}</section>`;
  const tabs = `<div class="tabs"><button data-t="general">📊 Resumen</button><button data-t="conciliar">🧾 Conciliar${faltan === null ? '' : `<span class="cnt${faltan ? '' : ' cero'}">${faltan || '✓'}</span>`}</button></div>`;
  const cuerpo = `<div class="w"><h1>💰 Finanzas</h1><div class="sub">${r ? rango(r) : ''}${p.cuenta ? ` · ${p.cuenta.toUpperCase()}` : ''}</div>${selector}${tabs}`
    + `<div class="panel" data-p="general">${tarjeta('resumen')}${categorias}${general.filter((id) => id !== 'resumen').map(tarjeta).join('')}</div><div class="panel" data-p="conciliar" hidden>${tarjeta('pendientes')}</div>`
    + '<div id="aviso"></div></div>';
  const js = `<script>
const q=${JSON.stringify(queryDe(p))};const av=document.getElementById('aviso');
function msg(t){av.textContent=t;av.style.display='block';clearTimeout(msg.t);msg.t=setTimeout(()=>av.style.display='none',3500)}
document.addEventListener('click',async e=>{const b=e.target.closest('button[data-r]');if(!b)return;b.disabled=true;const t=b.textContent;b.textContent='Enviando…';
try{const r=await fetch('enviar?reporte='+b.dataset.r+'&'+q);const j=await r.json();msg(j.mensaje||j.error||'Listo')}catch{msg('No se pudo enviar')}
b.disabled=false;b.textContent=t});
function tab(t){document.querySelectorAll('.tabs button').forEach(b=>b.classList.toggle('on',b.dataset.t===t));document.querySelectorAll('.panel').forEach(p=>p.hidden=p.dataset.p!==t);try{localStorage.setItem('tab',t)}catch{}}
document.querySelectorAll('.tabs button').forEach(b=>b.onclick=()=>tab(b.dataset.t));
let t0='general';try{t0=location.hash==='#conciliar'?'conciliar':localStorage.getItem('tab')||'general'}catch{}tab(t0==='conciliar'?'conciliar':'general');
</script>`;
  void q;
  return pagina('Finanzas', cuerpo, { extra: js });
}


const rotuloFiltro = (f) => {
  const nombres = nombresTipo();
  return [f.rubro ? rubroNombre(f.rubro) : '', f.tipo ? (nombres.get(f.tipo) ?? f.tipo) : '', f.clase === 'ingreso' ? 'Ingresos' : f.clase === 'gasto' ? 'Gastos' : f.tarjeta ? 'Tarjeta' : 'Balance', f.tarjeta ? `tarjeta …${f.tarjeta}` : '', f.moneda ?? ''].filter(Boolean).join(' · ');
};

/** Vista de detalle: los movimientos que forman un monto del dashboard, con total, «Enviar al chat» y «Atrás». */
export async function htmlDetalle(p, f, { imagen = false } = {}) {
  const args = { ...p.args };
  if (f.desde || f.hasta) { delete args.ciclo; delete args.mes; args.desde = f.desde; args.hasta = f.hasta; }
  if (f.rubro) args.de = f.rubro;
  for (const k of ['clase', 'tipo', 'tarjeta']) if (f[k]) args[k] = f[k];
  const titulo = rotuloFiltro(f);
  let m;
  try { m = await movimientos.handler(args); } catch (e) { return pagina(titulo, `<div class="w"><h1>${esc(titulo)}</h1>${aviso(e.message)}</div>`, { imagen }); }
  const nombres = nombresTipo();
  const lista = m.movimientos.filter((x) => !f.moneda || x.moneda === f.moneda);
  const money_ = imagen ? money : moneyCorto;
  const firmado = (x) => (f.clase || x.clase === 'ingreso' ? x.total : -x.total);
  const porMoneda = [...Object.entries(Object.groupBy(lista, (x) => x.moneda))].map(([mo, xs]) => [mo, xs.reduce((s, x) => s + firmado(x), 0), xs.length]);
  const filas = lista.slice(0, imagen ? MAX_FILAS.imagen : 500).map((x) => [
    `<span class="fe">${fechaCorta(x.fecha)}</span> ${esc(x.comercio || x.nota || '—')}`,
    esc(x.cuenta.toUpperCase()),
    esc([nombres.get(x.tipo) ?? x.tipo, x.rubro !== HOGAR ? rubroNombre(x.rubro) : ''].filter(Boolean).join(' · ')),
    `<span class="${x.clase === 'ingreso' ? 'm ingreso' : ''}">${money_(firmado(x), x.moneda)}</span>`,
  ]);
  const totales = porMoneda.map(([mo, t, n]) => [`Total ${mo} · ${n} mov.`, '', '', money_(t, mo)]);
  const cuerpoTabla = lista.length
    ? tabla(['Movimiento', 'Cuenta', 'Tipo', 'Monto'], filas, { total: totales[0], clase: 'det' }).replace('</tbody>', `${totales.slice(1).map((t) => `<tr class="t">${t.map((c, i) => `<td${i ? ' class="n"' : ''}>${c}</td>`).join('')}</tr>`).join('')}</tbody>`)
      + (lista.length > (imagen ? MAX_FILAS.imagen : 500) ? `<p class="vacio">Se muestran ${imagen ? MAX_FILAS.imagen : 500} de ${lista.length}.</p>` : '')
    : '<p class="vacio">Sin movimientos para este monto.</p>';
  const est = Number(f.estado);
  const anotado = porMoneda.length === 1 ? -porMoneda[0][1] : null;
  const nota = f.tarjeta ? `<div class="pedir" style="background:var(--ch);color:var(--fg)"><span>Estado del banco${f.moneda && anotado !== null ? ` · falta anotar ${money_(est - anotado, f.moneda)}` : ''}</span><b style="color:var(--fg)">${money_(est, f.moneda ?? 'CRC')}</b></div><p class="sub">Los movimientos son los anotados en el periodo del corte; el estado del banco incluye lo que aún no está anotado.</p>` : '';
  const sub = `${rango(m)}${p.cuenta ? ` · ${p.cuenta.toUpperCase()}` : ''}`;
  const botones = imagen ? '' : `<div class="acc"><button class="btn sec" id="atras">◀ Atrás</button><button class="btn" id="enviar">Enviar al chat</button></div>`;
  const js = imagen ? '<script>document.documentElement.dataset.alto=document.documentElement.scrollHeight</script>'
    : `<script>
const q=${JSON.stringify(queryDe(p, queryFiltro(f)))};const av=document.getElementById('aviso');
function msg(t){av.textContent=t;av.style.display='block';clearTimeout(msg.t);msg.t=setTimeout(()=>av.style.display='none',3500)}
document.getElementById('atras').onclick=()=>{if(history.length>1)history.back();else location.href='./?'+${JSON.stringify(queryDe(p))}};
document.getElementById('enviar').onclick=async e=>{const b=e.currentTarget;b.disabled=true;const t=b.textContent;b.textContent='Enviando…';
try{const r=await fetch('enviar?reporte=detalle&'+q);const j=await r.json();msg(j.mensaje||j.error||'Listo')}catch{msg('No se pudo enviar')}
b.disabled=false;b.textContent=t};
</script>`;
  const cuerpo = `<div class="w">${botones}<h1>${esc(titulo)}</h1><div class="sub">${sub}</div>${nota}<section class="card">${cuerpoTabla}</section><div id="aviso"></div></div>`;
  return pagina(titulo, cuerpo, { imagen, extra: js });
}

export const tituloDetalle = (f) => rotuloFiltro(f);


// ── Niveles: ciclo → categoría → movimiento → factura (modelo de 3 niveles, ADR-EBC-03) ──
const CATEGORIA_ID = /^[\w.-]{1,40}$/;
/** Condición SQL del periodo y la cuenta sobre una tabla con columnas ciclo, fecha y cuenta. */
function condicionPeriodo(p, alias) {
  const c = []; const a = [];
  if (p.modo === 'ciclo') { c.push(`${alias}.ciclo = ?`); a.push(p.ciclo); }
  else if (p.modo === 'mes') { c.push(`substr(${alias}.fecha, 1, 7) = ?`); a.push(p.mes); }
  else { if (p.desde) { c.push(`${alias}.fecha >= ?`); a.push(p.desde); } if (p.hasta) { c.push(`${alias}.fecha <= ?`); a.push(p.hasta); } }
  if (p.cuenta) { c.push(`${alias}.cuenta = ?`); a.push(p.cuenta); }
  return { where: c.length ? c.join(' AND ') : '1=1', args: a };
}
/** consultar() del MCP no admite parámetros: se escapan los textos y los números ya vienen validados. */
const sqlTexto = (t) => `'${String(t).replace(/'/g, "''")}'`;
const conArgs = (sql, args) => { let i = 0; return sql.replace(/\?/g, () => sqlTexto(args[i++])); };
const filasDe = (sql, args = []) => consultar(conArgs(sql, args), 5000).filas;

/** Nivel 2: ingresos y gastos por categoría (partidas). Un movimiento repartido aparece en cada categoría con su monto. */
function seccionCategorias(p) {
  try {
    const nombres = nombresTipo();
    const { where, args } = condicionPeriodo(p, 'c');
    const filas = filasDe(`SELECT c.clase, c.moneda, c.tipo, round(sum(c.monto), 2) AS total, count(DISTINCT c.movimiento_id) AS n FROM movimiento_categorias c WHERE ${where} GROUP BY 1, 2, 3 ORDER BY 1 DESC, 2, 4 DESC`, args);
    if (!filas.length) return '<p class="vacio">Sin movimientos en este periodo.</p>';
    const grupos = Object.groupBy(filas, (x) => `${x.clase}|${x.moneda}`);
    return Object.entries(grupos).map(([k, lista]) => {
      const [clase, moneda] = k.split('|');
      const suma = lista.reduce((s, x) => s + x.total, 0);
      const href = (x) => `categoria.html?${queryDe(p, { clase, moneda, tipo: x.tipo })}`;
      return `<h3>${clase === 'ingreso' ? 'Ingresos' : 'Gastos'} · ${moneda}</h3>${tabla(['Categoría', 'Mov.', 'Monto'],
        lista.map((x) => [`<a class="mt" href="${href(x)}">${esc(nombres.get(x.tipo) ?? x.tipo)}</a>`, String(x.n), `<a class="mt" href="${href(x)}">${moneyCorto(x.total, moneda)}</a>`]),
        { total: [`Total ${moneda}`, '', moneyCorto(suma, moneda)] })}`;
    }).join('');
  } catch (e) { return aviso(e.message); }
}

const botonesNivel = (imagen, volver) => (imagen ? '' : `<div class="acc"><button class="btn sec" id="atras">◀ Atrás</button><button class="btn" id="enviar">Enviar al chat</button></div>`);
/** Script de «Atrás» y «Enviar al chat» de los niveles; q = query de la propia pantalla. */
const jsNivel = (imagen, id, q, volver) => (imagen ? '<script>document.documentElement.dataset.alto=document.documentElement.scrollHeight</script>'
  : `<script>
const q=${JSON.stringify(q)};const av=document.getElementById('aviso');
function msg(t){av.textContent=t;av.style.display='block';clearTimeout(msg.t);msg.t=setTimeout(()=>av.style.display='none',3500)}
document.getElementById('atras').onclick=()=>{if(history.length>1)history.back();else location.href=${JSON.stringify(volver)}};
document.getElementById('enviar').onclick=async e=>{const b=e.currentTarget;b.disabled=true;const t=b.textContent;b.textContent='Enviando…';
try{const r=await fetch('enviar?reporte=${id}&'+q);const j=await r.json();msg(j.mensaje||j.error||'Listo')}catch{msg('No se pudo enviar')}
b.disabled=false;b.textContent=t};
</script>`);

/** Nivel 2 → movimientos del nivel 1 de una categoría, con el monto de la partida y el total del movimiento. */
export async function htmlCategoria(p, q, { imagen = false } = {}) {
  const clase = q.get('clase') === 'ingreso' ? 'ingreso' : 'gasto';
  const moneda = q.get('moneda') === 'USD' ? 'USD' : 'CRC';
  const tipo = CATEGORIA_ID.test(q.get('tipo') ?? '') ? q.get('tipo') : 'otros';
  const nombre = nombresTipo().get(tipo) ?? tipo;
  const titulo = `${clase === 'ingreso' ? 'Ingresos' : 'Gastos'} · ${nombre} · ${moneda}`;
  const volver = `./?${queryDe(p)}`;
  const m_ = imagen ? money : moneyCorto;
  let cuerpoTabla;
  try {
    const { where, args } = condicionPeriodo(p, 'c');
    const filas = filasDe(`SELECT c.movimiento_id, c.fecha, c.cuenta, c.comercio, c.monto, m.total, (SELECT count(*) FROM facturas f WHERE f.libro_id = c.libro_id) AS facturas FROM movimiento_categorias c JOIN movimientos m ON m.id = c.movimiento_id WHERE ${where} AND c.clase = ? AND c.moneda = ? AND c.tipo = ? ORDER BY c.fecha DESC, c.movimiento_id`, [...args, clase, moneda, tipo]);
    const suma = filas.reduce((s, x) => s + x.monto, 0);
    const link = (x, html) => (imagen ? html : `<a class="mt" href="movimiento.html?${queryDe(p, { id: x.movimiento_id })}">${html}</a>`);
    cuerpoTabla = filas.length
      ? tabla(['Movimiento', 'Cuenta', 'Total', 'Partida'], filas.slice(0, imagen ? MAX_FILAS.imagen : 500).map((x) => [
        `<span class="fe">${fechaCorta(x.fecha)}</span> ${link(x, esc(x.comercio || '—'))}${x.facturas ? ' 🧾' : ''}`,
        esc(x.cuenta.toUpperCase()), m_(x.total, moneda), `<span class="${clase === 'ingreso' ? 'm ingreso' : ''}">${m_(x.monto, moneda)}</span>`]),
      { total: [`Total · ${filas.length} mov.`, '', '', m_(suma, moneda)], clase: 'det' })
      : '<p class="vacio">Sin movimientos en esta categoría.</p>';
  } catch (e) { cuerpoTabla = aviso(e.message); }
  const sub = `${p.modo === 'ciclo' ? `Ciclo ${p.ciclo}` : p.modo === 'mes' ? p.mes : 'Rango'}${p.cuenta ? ` · ${p.cuenta.toUpperCase()}` : ''}`;
  const cuerpo = `<div class="w">${botonesNivel(imagen)}<h1>${esc(titulo)}</h1><div class="sub">${sub} · 🧾 = tiene factura</div><section class="card">${cuerpoTabla}</section><div id="aviso"></div></div>`;
  return pagina(titulo, cuerpo, { imagen, extra: jsNivel(imagen, 'categoria', queryDe(p, { clase, moneda, tipo }), volver) });
}

/** Nivel 1 → partidas (nivel 2) y facturas con sus líneas, ajustes e imagen (nivel 3). Sin nivel 3 muestra solo movimiento y partidas. */
export async function htmlMovimiento(p, q, { imagen = false } = {}) {
  const id = /^[\w.-]{1,40}$/.test(q.get('id') ?? '') ? q.get('id') : '';
  const nombres = nombresTipo();
  const volver = `./?${queryDe(p)}`;
  let titulo = 'Movimiento'; let cuerpoMov = ''; let sub = '';
  try {
    const m = filasDe('SELECT * FROM movimientos WHERE id = ?', [id])[0];
    if (!m) throw new Error('No existe ese movimiento.');
    const m_ = imagen ? money : moneyCorto;
    const firmado = m.clase === 'ingreso' ? m.total : -m.total;
    titulo = m.comercio || m.nota || 'Movimiento';
    sub = `${fechaCorta(m.fecha)} · ${m.cuenta.toUpperCase()} · ${m.clase === 'ingreso' ? 'Ingreso' : 'Gasto'}${m.referencia ? ` · ref. ${m.referencia}` : ''}`;
    const partidas = m.libro_id ? filasDe('SELECT * FROM libro_partidas WHERE libro_id = ? ORDER BY n', [m.libro_id]) : [];
    const partidasHtml = tabla(['Categoría', 'Monto'], (partidas.length ? partidas : [{ tipo: m.tipo || 'otros', monto: m.total }]).map((x) => [esc(nombres.get(x.tipo) ?? x.tipo) + (x.nota ? `<small>${esc(x.nota)}</small>` : ''), m_(x.monto, m.moneda)]), { total: ['Total del movimiento', m_(m.total, m.moneda)] });
    const nota = m.nota ? `<p class="sub">${esc(m.nota)}</p>` : '';
    const facturas = m.libro_id ? filasDe('SELECT * FROM facturas WHERE libro_id = ? ORDER BY fecha, id', [m.libro_id]) : [];
    const facturasHtml = facturas.map((f) => {
      const lineas = filasDe('SELECT * FROM factura_lineas WHERE factura_id = ? ORDER BY n', [f.id]);
      const ajustes = filasDe('SELECT * FROM factura_ajustes WHERE factura_id = ? ORDER BY n', [f.id]);
      const filas = lineas.map((l) => [
        `${esc(l.descripcion || '—')}${l.cantidad ? `<small>${l.cantidad}${l.unidad ? ` ${esc(l.unidad)}` : ''}${l.precio ? ` × ${m_(l.precio, f.moneda)}` : ''}</small>` : ''}`,
        l.impuesto ? m_(l.impuesto, f.moneda) : '', esc(nombres.get(l.tipo) ?? l.tipo ?? '—'), m_(l.total, f.moneda)]);
      const extra = ajustes.map((a) => [esc(a.descripcion || 'Ajuste'), '', 'Ajuste', m_(a.total, f.moneda)]);
      const ver = !imagen && f.archivo ? `<a class="btn sec" href="archivo?f=${encodeURIComponent(f.id)}" target="_blank" rel="noopener">Ver imagen</a>` : '';
      return `<section class="card"><div class="cab"><h2>${esc(f.proveedor || 'Factura')}</h2>${ver}</div>`
        + `<p class="sub">${[f.cedula ? `Céd. jurídica ${esc(f.cedula)}` : '', f.numero ? `N.º ${esc(f.numero)}` : '', fechaCorta(f.fecha)].filter(Boolean).join(' · ')}</p>`
        + (filas.length || extra.length ? tabla(['Producto', 'Impuesto', 'Categoría', 'Total'], [...filas, ...extra], { total: ['Total factura', '', '', m_(f.total, f.moneda)], clase: 'det' }) : `<p class="vacio">Sin líneas. Total ${m_(f.total, f.moneda)}.</p>`)
        + '</section>';
    }).join('');
    cuerpoMov = `<section class="card"><div class="cab"><h2>${m_(firmado, m.moneda)}</h2></div>${nota}${partidasHtml}</section>`
      + (facturas.length ? `<h3>Facturas (${facturas.length})</h3>${facturasHtml}` : '<p class="vacio">Sin detalle de factura.</p>');
  } catch (e) { cuerpoMov = aviso(e.message); }
  const cuerpo = `<div class="w">${botonesNivel(imagen)}<h1>${esc(titulo)}</h1><div class="sub">${sub}</div>${cuerpoMov}<div id="aviso"></div></div>`;
  return pagina(titulo, cuerpo, { imagen, extra: jsNivel(imagen, 'movimiento', queryDe(p, { id }), volver) });
}

const TIPOS_ARCHIVO = { '.jpg': 'image/jpeg', '.jpeg': 'image/jpeg', '.png': 'image/png', '.webp': 'image/webp', '.heic': 'image/heic', '.pdf': 'application/pdf' };
/** Respaldo de una factura (imagen o PDF) desde datos/multimedia; null si no existe o sale de esa carpeta. */
export function archivoDeFactura(id) {
  if (!/^[\w.-]{1,40}$/.test(id ?? '')) return null;
  const f = filasDe('SELECT archivo FROM facturas WHERE id = ?', [id])[0];
  if (!f?.archivo) return null;
  const base = path.resolve(getDatosDir(), 'multimedia');
  const ruta = path.resolve(getDatosDir(), f.archivo);
  const tipo = TIPOS_ARCHIVO[path.extname(ruta).toLowerCase()];
  if (!ruta.startsWith(`${base}${path.sep}`) || !tipo || !existsSync(ruta)) return null;
  return { ruta, tipo };
}
