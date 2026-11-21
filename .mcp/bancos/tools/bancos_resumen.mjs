import { CUENTAS, HOGAR, RUBRO_PROPS, filtroRubro, leerDetalles, leerTodosLosMovimientos, plasticoUsado, r2, rubroDe, tarjetaDestino, tiposTraspaso } from '../src/datos.mjs';
import { periodoProps, rangoCiclo, resolverPeriodo } from '../src/periodo.mjs';

/** Suma un movimiento al acumulado de su clave. */
function sumar(mapa, clave, base, m) {
  const t = mapa.get(clave) ?? { ...base, ingresos: 0, gastos: 0, balance: 0, movimientos: 0, ids: new Set() };
  if (m.clase === 'ingreso') t.ingresos = r2(t.ingresos + m.total); else t.gastos = r2(t.gastos + m.total);
  t.balance = r2(t.ingresos - t.gastos);
  t.ids.add(m.id);
  t.movimientos = t.ids.size;
  mapa.set(clave, t);
}

/** Un movimiento se reparte en sus partidas (nivel 2): cada una con su categoría, su monto y su rubro. Sin libro, todo va a «otros». */
const partidasDe = (m) => (m.partidas?.length ? m.partidas : [{ tipo: 'otros', monto: m.total }])
  .map((p) => ({ ...m, tipo: p.tipo, total: p.monto, de: p.de ?? m.de }));

const sinIds = ({ ids: _i, ...t }) => t;

/** Filas por plástico (el titular primero) y, si la tarjeta tiene adicionales, su total: lo que se paga junto. */
function porTarjeta(filas) {
  const totales = new Map();
  for (const f of filas) {
    const k = `${f.tarjeta}|${f.moneda}`;
    const t = totales.get(k) ?? { tarjeta: f.tarjeta, plastico: 'total', moneda: f.moneda, ingresos: 0, gastos: 0, balance: 0, movimientos: 0, n: 0 };
    t.ingresos = r2(t.ingresos + f.ingresos); t.gastos = r2(t.gastos + f.gastos); t.balance = r2(t.ingresos - t.gastos);
    t.movimientos += f.movimientos; t.n += 1;
    totales.set(k, t);
  }
  const orden = (f) => (f.plastico === f.tarjeta ? 0 : f.plastico === 'total' ? 2 : 1);
  return [...filas, ...[...totales.values()].filter((t) => t.n > 1).map(({ n: _n, ...t }) => t)]
    .sort((a, b) => a.tarjeta.localeCompare(b.tarjeta) || a.moneda.localeCompare(b.moneda) || orden(a) - orden(b) || a.plastico.localeCompare(b.plastico));
}

/** Ciclo anterior si el periodo es un ciclo completo (del 19 al 18); si no, null. */
function cicloAnterior(desde, hasta) {
  if (!desde.endsWith('-19') || !hasta.endsWith('-18')) return null;
  const [a, m] = hasta.slice(0, 7).split('-').map(Number);
  const prev = m === 1 ? `${a - 1}-12` : `${a}-${String(m - 1).padStart(2, '0')}`;
  return rangoCiclo(prev);
}

/** Control de la deuda de tarjeta: compras del ciclo anterior contra lo pagado (traspasos) en este periodo, por tarjeta y moneda. */
function deudaTarjeta(desde, hasta, pagos) {
  const prev = cicloAnterior(desde, hasta);
  if (!prev) return [];
  const compras = new Map();
  for (const m of leerTodosLosMovimientos(prev.desde, prev.hasta, ['bac', 'bncr'])) {
    const k = `${m.cuenta}|${m.moneda}`;
    compras.set(k, r2((compras.get(k) ?? 0) + (m.clase === 'ingreso' ? -m.total : m.total)));
  }
  const claves = new Set([...compras.keys(), ...pagos.keys()]);
  return [...claves].sort().map((k) => {
    const [tarjeta, moneda] = k.split('|');
    const compra = compras.get(k);
    const pagado = pagos.get(k) ?? 0;
    return compra === undefined
      ? { tarjeta, moneda, comprasCicloAnterior: 'sin estado de cuenta', pagado, diferencia: null }
      : { tarjeta, moneda, comprasCicloAnterior: compra, pagado, diferencia: r2(compra - pagado), estado: compra - pagado > 0.005 ? 'pendiente' : compra - pagado < -0.005 ? 'a favor' : 'al dia' };
  });
}

export default {
  name: 'bancos_resumen',
  format: 'toon',
  order: 31,
  description:
    'Reporte de un ciclo (del 19 al 18, el periodo normal), mes o rango, sobre los movimientos bancarios (nivel 1, la base de verdad) repartidos en las partidas de su libro (nivel 2, la categoría; sin libro = «otros»): balance ingresos - gastos por moneda, ' +
    'balance por rubro (hogar = dueño y Karen; papa y otros por su de), totales por rubro, clase y tipo, ' +
    'y por tarjeta de crédito: lo de cada plástico (titular y adicionales) y el total que se paga junto. ' +
    'Por defecto incluye todo, también al papá. excluirDe: ["papa"] lo quita; de: "papa" deja solo lo suyo (para mostrárselo). ' +
    'Los traspasos (tipos con traspaso: true, como el pago de la tarjeta) no suman en balance, rubros ni tipos: salen aparte en «traspasos» (lo pagado a cada tarjeta) y «deudaTarjeta» (compras del ciclo anterior - pagado en este ciclo, solo si el periodo es un ciclo). incluirTraspasos: true los suma como gasto. ' +
    'excluirTipos quita otros tipos por id. ' +
    'También cuenta los libros (pagos) del periodo que siguen sin movimiento (no suman). Respuesta TOON.',
  inputSchema: {
    type: 'object',
    properties: {
      ...periodoProps,
      cuenta: { type: 'string', enum: CUENTAS, description: 'Default: todas.' },
      ...RUBRO_PROPS,
      excluirTipos: { type: 'array', items: { type: 'string' }, description: 'ids de tipo a excluir, p. ej. ["tarjeta"]. Default: ninguno.' },
      incluirTraspasos: { type: 'boolean', description: 'true = suma los traspasos (pago de tarjeta) como gasto. Default: false.' },
    },
    additionalProperties: false,
  },
  async handler(args) {
    const { desde, hasta } = resolverPeriodo(args);
    const fueraTipos = new Set(args.excluirTipos ?? []);
    const traspasos = tiposTraspaso();
    const todas = leerTodosLosMovimientos(desde, hasta, args.cuenta ? [args.cuenta] : CUENTAS)
      .flatMap(partidasDe)
      .filter(filtroRubro(args.de, args.excluirDe))
      .filter((m) => !fueraTipos.has(m.tipo));
    const movs = args.incluirTraspasos ? todas : todas.filter((m) => !traspasos.has(m.tipo));
    const pagos = new Map();
    const traspasosOut = new Map();
    for (const m of args.incluirTraspasos ? [] : todas.filter((x) => traspasos.has(x.tipo))) {
      const tarjeta = tarjetaDestino(m);
      const k = `${tarjeta}|${m.moneda}`;
      pagos.set(k, r2((pagos.get(k) ?? 0) + m.total));
      const t = traspasosOut.get(k) ?? { tarjeta, moneda: m.moneda, cantidad: 0, total: 0 };
      t.cantidad += 1; t.total = r2(t.total + m.total); traspasosOut.set(k, t);
    }
    const deuda = args.incluirTraspasos ? [] : deudaTarjeta(desde, hasta, pagos);
    const balance = new Map();
    const rubros = new Map();
    const tipos = new Map();
    const plasticos = new Map();
    for (const m of movs) {
      const rubro = rubroDe(m);
      sumar(balance, m.moneda, { moneda: m.moneda }, m);
      sumar(rubros, `${rubro}|${m.moneda}`, { rubro, moneda: m.moneda }, m);
      const k = `${rubro}|${m.clase}|${m.tipo}|${m.moneda}`;
      const t = tipos.get(k) ?? { rubro, clase: m.clase, tipo: m.tipo, moneda: m.moneda, cantidad: 0, total: 0 };
      t.cantidad += 1; t.total = r2(t.total + m.total); tipos.set(k, t);
      if (m.tarjeta) sumar(plasticos, `${m.tarjeta}|${plasticoUsado(m)}|${m.moneda}`, { tarjeta: m.tarjeta, plastico: plasticoUsado(m), moneda: m.moneda }, m);
    }
    const orden = (r) => (r === HOGAR ? '' : r);
    const pendientes = (() => {
      const p = new Map();
      for (const d of leerDetalles('libros', desde, hasta).filter((x) => !x.movimiento && !x.transito && (!args.cuenta || x.cuenta === args.cuenta))) {
        const e = p.get(d.moneda) ?? { nivel: 'libros', moneda: d.moneda, cantidad: 0, total: 0 };
        e.cantidad += 1; e.total = r2(e.total + d.total); p.set(d.moneda, e);
      }
      return [...p.values()];
    })();
    return {
      desde,
      hasta,
      cuenta: args.cuenta ?? 'todas',
      rubro: args.de ?? 'todos',
      excluidos: [...(args.excluirDe ?? []), ...fueraTipos].join(', ') || 'ninguno',
      movimientos: new Set(movs.map((m) => m.id)).size,
      balance: [...balance.values()].map(sinIds),
      rubros: [...rubros.values()].map(sinIds).sort((a, b) => orden(a.rubro).localeCompare(orden(b.rubro)) || a.moneda.localeCompare(b.moneda)),
      tipos: [...tipos.values()].sort((a, b) => orden(a.rubro).localeCompare(orden(b.rubro)) || a.clase.localeCompare(b.clase) || b.total - a.total),
      ...(traspasosOut.size ? { traspasos: [...traspasosOut.values()] } : {}),
      ...(deuda.length ? { deudaTarjeta: deuda } : {}),
      ...(plasticos.size ? { tarjetas: porTarjeta([...plasticos.values()].map(sinIds)) } : {}),
      ...(pendientes.length ? { pendientesSinMovimiento: pendientes } : {}),
    };
  },
  async smoke({ callTool, check, toolText }) {
    const text = toolText(await callTool('bancos_resumen', { mes: '2026-10' }));
    check('bancos_resumen calcula el balance en CRC', text.includes('CRC,500000,22500.5,477499.5,5'), text.slice(0, 400));
    check('bancos_resumen separa al papá como rubro', /hogar,CRC,500000,14000.5,485999.5,4/.test(text) && /papa,CRC,0,8500,-8500,1/.test(text), text.slice(0, 600));
    check('bancos_resumen reparte el movimiento en las categorías de sus partidas', /gasto,supermercado,CRC,\d,\d+/.test(text) && /gasto,comida,CRC,\d,\d+/.test(text), text.slice(0, 900));
    const sinPapa = toolText(await callTool('bancos_resumen', { mes: '2026-10', excluirDe: ['papa'] }));
    check('bancos_resumen excluye al papá con excluirDe', sinPapa.includes('CRC,500000,14000.5,485999.5,4') && !/papa,CRC/.test(sinPapa), sinPapa.slice(0, 400));
    const papa = toolText(await callTool('bancos_resumen', { mes: '2026-10', de: 'papa' }));
    check('bancos_resumen da solo lo del papá', /movimientos: 1/.test(papa) && papa.includes('CRC,0,8500,-8500,1'), papa.slice(0, 300));
    const bac = toolText(await callTool('bancos_resumen', { mes: '2026-10', cuenta: 'bac', excluirTipos: ['comida'] }));
    check('bancos_resumen excluye una categoría solo por la parte que le toca', /CRC,0,9500,-9500,2/.test(bac), bac.slice(0, 300));
    const nov = toolText(await callTool('bancos_resumen', { mes: '2026-11' }));
    const ciclo = toolText(await callTool('bancos_resumen', { ciclo: '2026-01' }));
    check('bancos_resumen por ciclo va del 19 al 18', ciclo.includes('desde: 2025-12-19') && ciclo.includes('hasta: 2026-01-18'), ciclo.slice(0, 200));
    check('bancos_resumen del retiro: 5000 en 2 partidas (3000 comida, 2000 supermercado)', /gasto,comida,CRC,1,3000/.test(nov) && /gasto,supermercado,CRC,\d,(2000|5500)/.test(nov), nov.slice(0, 900));
    const compras = [
      { fecha: '2026-08-10', clase: 'gasto', tipo: 'comida', total: 30000, moneda: 'CRC', comercio: 'Soda Z', tarjeta: '1111' },
      { fecha: '2026-08-12', clase: 'gasto', tipo: 'supermercado', total: 20000, moneda: 'CRC', comercio: 'Súper Z', tarjeta: '1111' },
    ];
    await callTool('bancos_registrar', { cuenta: 'bac', movimientos: compras, apply: true });
    await callTool('bancos_registrar', { cuenta: 'bcr', apply: true, movimientos: [{ fecha: '2026-09-05', clase: 'gasto', tipo: 'tarjeta', total: 45000, moneda: 'CRC', comercio: 'BCR transferencia', nota: 'Pago tarjetas BAC' }] });
    const sep = toolText(await callTool('bancos_resumen', { ciclo: '2026-09' }));
    check('bancos_resumen no suma el pago de tarjeta como gasto', !/tarjeta,CRC,1,45000/.test(sep) && !/CRC,0,45000/.test(sep), sep.slice(0, 500));
    check('bancos_resumen saca el pago en traspasos', /traspasos/.test(sep) && /bac,CRC,1,45000/.test(sep), sep.slice(0, 700));
    check('bancos_resumen calcula la deuda de tarjeta: 50000 de compras - 45000 pagado = 5000 pendiente', /deudaTarjeta/.test(sep) && /bac,CRC,50000,45000,5000,pendiente/.test(sep), sep.slice(0, 900));
    const conTraspaso = toolText(await callTool('bancos_resumen', { ciclo: '2026-09', incluirTraspasos: true }));
    check('bancos_resumen con incluirTraspasos suma el pago como gasto', /tarjeta,CRC,1,45000/.test(conTraspaso) && !/traspasos/.test(conTraspaso), conTraspaso.slice(0, 500));
    const sinEstado = toolText(await callTool('bancos_resumen', { ciclo: '2026-10' }));
    check('bancos_resumen sin compras del ciclo anterior no inventa deuda', !/deudaTarjeta/.test(sinEstado) || !/bac,CRC,\d/.test(sinEstado.split('deudaTarjeta')[1] ?? ''), sinEstado.slice(0, 300));
    check('bancos_resumen cuenta el pago pendiente sin movimiento aparte', !/pendientesSinMovimiento/.test(nov), nov.slice(-300));
  },
};
