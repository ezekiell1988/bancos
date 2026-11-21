// HTML del dashboard y de los reportes. Una sola vista sirve a la pantalla y a la imagen (img=1: ancho fijo 1080 px, fuente grande).
// Los números salen de los mismos handlers que los tools bancos_* del MCP, así cuadran con lo que ve el agente.
import movimientos from '../.mcp/bancos/tools/bancos_movimientos.mjs';
import pagoTarjetas from '../.mcp/bancos/tools/bancos_pago_tarjetas.mjs';
import pendientes from '../.mcp/bancos/tools/bancos_pendientes.mjs';
import resumen from '../.mcp/bancos/tools/bancos_resumen.mjs';
import { CUENTAS, HOGAR, leerTipos } from '../.mcp/bancos/src/datos.mjs';
import { cicloDe, rangoCiclo } from '../.mcp/bancos/src/periodo.mjs';
import { esc, fechaCorta, fmt } from '../.mcp/bancos/src/pago.mjs';

export const REPORTES = {
  resumen: 'Resumen del periodo',
  tipos: 'Gastos por tipo',
  pago: 'Pago de contado de las tarjetas',
  pendientes: 'Pendientes de conciliar',
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
  const ciclo = /^\d{4}-(0[1-9]|1[0-2])$/.test(q.get('ciclo') ?? '') ? q.get('ciclo') : cicloDe(hoyCR());
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
const rango = (r) => `${fechaCorta(r.desde)} al ${fechaCorta(r.hasta)}`;

function tabla(cabeza, filas, { total = null } = {}) {
  return `<div class="tw"><table${cabeza.length > 3 ? ' class="ap"' : ''}><thead><tr>${cabeza.map((c, i) => `<th${i ? ' class="n"' : ''}>${esc(c)}</th>`).join('')}</tr></thead><tbody>`
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
  return { p, r, m, pe, pago, max: imagen ? MAX_FILAS.imagen : MAX_FILAS.pantalla, money: imagen ? money : moneyCorto };
}

const aviso = (e) => `<p class="vacio">No se pudo cargar: ${esc(e)}</p>`;

const secciones = {
  resumen(d) {
    if (d.r.error) return aviso(d.r.error);
    const r = d.r.ok;
    const bal = r.balance.map((b) => `<div class="chips"><div class="chip"><span>Ingresos ${b.moneda}</span><b>${d.money(b.ingresos, b.moneda)}</b></div>`
      + `<div class="chip"><span>Gastos ${b.moneda}</span><b>${d.money(b.gastos, b.moneda)}</b></div>`
      + `<div class="chip ${b.balance < 0 ? 'alerta' : 'bien'}"><span>Balance ${b.moneda}</span><b>${d.money(b.balance, b.moneda)}</b></div></div>`).join('');
    const rubros = r.rubros.length ? tabla(['Rubro', 'Ingresos', 'Gastos', 'Balance'],
      r.rubros.map((x) => [`${esc(rubroNombre(x.rubro))} <small>${x.moneda} · ${x.movimientos} mov.</small>`, d.money(x.ingresos, x.moneda), d.money(x.gastos, x.moneda), `<b>${d.money(x.balance, x.moneda)}</b>`])) : '';
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
        lista.map((t) => [esc(nombres.get(t.tipo) ?? t.tipo), String(t.cantidad), d.money(t.total, moneda)]), { total: ['Total', String(lista.reduce((s, t) => s + t.cantidad, 0)), d.money(suma, moneda)] })}`;
    }).join('');
  },
  pago(d) {
    if (d.pago.error) return aviso(d.pago.error);
    const g = d.pago.ok;
    const filas = g.tarjetas.map((t) => [`<b>${esc(t.plan)}</b><small>${esc(t.marca)} …${esc(t.tarjeta)}</small>`, d.money(t.colones, 'CRC'), d.money(t.dolares, 'USD'), `<b>${d.money(t.totalColones, 'CRC')}</b>`]);
    return `<p class="sub">Estado del corte ${fechaCorta(g.corte)} · límite ${fechaCorta(g.limite)} · dólar ${d.money(g.tipoCambio.venta, 'CRC')}</p>`
      + tabla(['Tarjeta', 'Colones', 'Dólares', 'Total en ₡'], filas, { total: ['Total', d.money(g.total.colones, 'CRC'), d.money(g.total.dolares, 'USD'), d.money(g.total.totalColones, 'CRC')] })
      + `<div class="pedir"><span>Falta por pagar${g.pagosDesdeCorte.length ? ` (ya se pagó ${d.money(g.pagosDesdeCorte.reduce((s, x) => s + x.total, 0), 'CRC')})` : ''}</span><b>${d.money(g.pedirAKaren, 'CRC')}</b></div>`;
  },
  pendientes(d) {
    if (d.pe.error) return aviso(d.pe.error);
    const pe = d.pe.ok;
    const sinDetalle = pe.movimientosSinDetalle?.lista ?? [];
    const sinMov = pe.detallesSinMovimiento?.lista ?? [];
    const saldo = pe.facturasConSaldo ?? [];
    const lista = (titulo, filas) => (filas.length ? `<h3>${titulo} (${filas.length})</h3><div class="lista">${filas.slice(0, d.max).map((f) => f).join('')}${filas.length > d.max ? `<p class="vacio">y ${filas.length - d.max} más</p>` : ''}</div>` : '');
    const fila = (fecha, texto, sub, monto) => `<div class="f"><div class="d"><span class="fe">${fechaCorta(fecha)}</span> ${esc(texto)}${sub ? `<small>${esc(sub)}</small>` : ''}</div><div class="m">${monto}</div></div>`;
    const html = lista('Movimientos sin comprobante ni factura', sinDetalle.map((x) => fila(x.fecha, x.comercio || x.tipo, `${x.cuenta.toUpperCase()} · ${x.clase}`, d.money(x.total, x.moneda))))
      + lista('Comprobantes y facturas sin movimiento', sinMov.map((x) => fila(x.fecha, x.comercio, `${x.nivel === 2 ? 'Comprobante' : 'Factura'}${x.referencia ? ` ${x.referencia}` : ''}`, d.money(x.total, x.moneda))))
      + lista('Facturas con saldo', saldo.map((x) => fila(x.fecha, x.comercio, `Pagado ${d.money(x.pagado ?? 0, x.moneda)}`, `saldo ${d.money(x.saldo, x.moneda)}`)));
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
.vacio{color:var(--mu);margin:8px 0}
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
    + (p.modo === 'ciclo' ? `<div class="bar">${enlace('◀', { ciclo: sumarCiclo(ciclo, -1) })}${enlace('Actual', {})}${enlace('▶', { ciclo: sumarCiclo(ciclo, 1) })}</div>` : '')
    + (p.modo === 'mes' ? `<div class="bar">${enlace('◀', { mes: sumarCiclo(p.mes, -1) })}${enlace('Hoy', { mes: hoyCR().slice(0, 7) })}${enlace('▶', { mes: sumarCiclo(p.mes, 1) })}</div>` : '')
    + (p.modo === 'rango' ? `<form class="rg"><input type="date" name="desde" value="${p.desde ?? ''}"><input type="date" name="hasta" value="${p.hasta ?? ''}">${p.cuenta ? `<input type="hidden" name="cuenta" value="${p.cuenta}">` : ''}<button class="btn">Ver</button></form>` : '')
    + `<div class="bar"><a href="?${queryDe({ ...p, cuenta: undefined })}"${p.cuenta ? '' : ' class="on"'}>Todas</a>${CUENTAS.map((c) => `<a href="?${queryDe({ ...p, cuenta: c })}"${p.cuenta === c ? ' class="on"' : ''}>${c.toUpperCase()}</a>`).join('')}</div>`;
  const tarjeta = (id) => `<section class="card"><div class="cab"><h2>${esc(REPORTES[id])}</h2><button class="btn" data-r="${id}">Enviar al chat</button></div>${secciones[id](d)}</section>`;
  const cuerpo = `<div class="w"><h1>💰 Finanzas</h1><div class="sub">${r ? rango(r) : ''}${p.cuenta ? ` · ${p.cuenta.toUpperCase()}` : ''}</div>${selector}`
    + Object.keys(REPORTES).map(tarjeta).join('') + '<div id="aviso"></div></div>';
  const js = `<script>
const q=${JSON.stringify(queryDe(p))};const av=document.getElementById('aviso');
function msg(t){av.textContent=t;av.style.display='block';clearTimeout(msg.t);msg.t=setTimeout(()=>av.style.display='none',3500)}
document.addEventListener('click',async e=>{const b=e.target.closest('button[data-r]');if(!b)return;b.disabled=true;const t=b.textContent;b.textContent='Enviando…';
try{const r=await fetch('enviar?reporte='+b.dataset.r+'&'+q);const j=await r.json();msg(j.mensaje||j.error||'Listo')}catch{msg('No se pudo enviar')}
b.disabled=false;b.textContent=t});
</script>`;
  void q;
  return pagina('Finanzas', cuerpo, { extra: js });
}
