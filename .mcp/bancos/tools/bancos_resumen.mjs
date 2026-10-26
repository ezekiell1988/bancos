import { CUENTAS, HOGAR, RUBRO_PROPS, filtroRubro, leerDetalles, leerTodosLosMovimientos, r2, rubroDe } from '../src/datos.mjs';
import { periodoProps, resolverPeriodo } from '../src/periodo.mjs';
import { pagosDe } from '../src/vinculos.mjs';

/** Suma un movimiento al acumulado de su clave. */
function sumar(mapa, clave, base, m) {
  const t = mapa.get(clave) ?? { ...base, ingresos: 0, gastos: 0, balance: 0, movimientos: 0 };
  if (m.clase === 'ingreso') t.ingresos = r2(t.ingresos + m.total); else t.gastos = r2(t.gastos + m.total);
  t.balance = r2(t.ingresos - t.gastos);
  t.movimientos += 1;
  mapa.set(clave, t);
}

export default {
  name: 'bancos_resumen',
  format: 'toon',
  order: 31,
  description:
    'Reporte de un mes o rango, solo con movimientos bancarios (nivel 1, la base de verdad): balance ingresos - gastos por moneda, ' +
    'balance por rubro (hogar = dueño y Karen; papa y otros por su de) y totales por rubro, clase y tipo. ' +
    'Por defecto incluye todo, también al papá. excluirDe: ["papa"] lo quita; de: "papa" deja solo lo suyo (para mostrárselo). ' +
    'excluirTipos: ["tarjeta"] da el gasto real sin el pago de la tarjeta BAC desde el BCR. ' +
    'También cuenta los comprobantes y facturas del periodo que siguen sin movimiento (no suman). Respuesta TOON.',
  inputSchema: {
    type: 'object',
    properties: {
      ...periodoProps,
      cuenta: { type: 'string', enum: CUENTAS, description: 'Default: todas.' },
      ...RUBRO_PROPS,
      excluirTipos: { type: 'array', items: { type: 'string' }, description: 'ids de tipo a excluir, p. ej. ["tarjeta"]. Default: ninguno.' },
    },
    additionalProperties: false,
  },
  async handler(args) {
    const { desde, hasta } = resolverPeriodo(args);
    const fueraTipos = new Set(args.excluirTipos ?? []);
    const movs = leerTodosLosMovimientos(desde, hasta, args.cuenta ? [args.cuenta] : CUENTAS)
      .filter(filtroRubro(args.de, args.excluirDe))
      .filter((m) => !fueraTipos.has(m.tipo));
    const balance = new Map();
    const rubros = new Map();
    const tipos = new Map();
    for (const m of movs) {
      const rubro = rubroDe(m);
      sumar(balance, m.moneda, { moneda: m.moneda }, m);
      sumar(rubros, `${rubro}|${m.moneda}`, { rubro, moneda: m.moneda }, m);
      const k = `${rubro}|${m.clase}|${m.tipo}|${m.moneda}`;
      const t = tipos.get(k) ?? { rubro, clase: m.clase, tipo: m.tipo, moneda: m.moneda, cantidad: 0, total: 0 };
      t.cantidad += 1; t.total = r2(t.total + m.total); tipos.set(k, t);
    }
    const orden = (r) => (r === HOGAR ? '' : r);
    const pendientes = ['comprobantes', 'facturas'].flatMap((nivel) => {
      const p = new Map();
      for (const d of leerDetalles(nivel, desde, hasta).filter((x) => !x.movimiento && !pagosDe(x).length && (!args.cuenta || x.cuenta === args.cuenta))) {
        const e = p.get(d.moneda) ?? { nivel, moneda: d.moneda, cantidad: 0, total: 0 };
        e.cantidad += 1; e.total = r2(e.total + d.total); p.set(d.moneda, e);
      }
      return [...p.values()];
    });
    return {
      desde,
      hasta,
      cuenta: args.cuenta ?? 'todas',
      rubro: args.de ?? 'todos',
      excluidos: [...(args.excluirDe ?? []), ...fueraTipos].join(', ') || 'ninguno',
      movimientos: movs.length,
      balance: [...balance.values()],
      rubros: [...rubros.values()].sort((a, b) => orden(a.rubro).localeCompare(orden(b.rubro)) || a.moneda.localeCompare(b.moneda)),
      tipos: [...tipos.values()].sort((a, b) => orden(a.rubro).localeCompare(orden(b.rubro)) || a.clase.localeCompare(b.clase) || b.total - a.total),
      ...(pendientes.length ? { pendientesSinMovimiento: pendientes } : {}),
    };
  },
  async smoke({ callTool, check, toolText }) {
    const text = toolText(await callTool('bancos_resumen', { mes: '2026-10' }));
    check('bancos_resumen calcula el balance en CRC', text.includes('CRC,500000,22500.5,477499.5,5'), text.slice(0, 400));
    check('bancos_resumen separa al papá como rubro', /hogar,CRC,500000,14000.5,485999.5,4/.test(text) && /papa,CRC,0,8500,-8500,1/.test(text), text.slice(0, 600));
    const sinPapa = toolText(await callTool('bancos_resumen', { mes: '2026-10', excluirDe: ['papa'] }));
    check('bancos_resumen excluye al papá con excluirDe', sinPapa.includes('CRC,500000,14000.5,485999.5,4') && !/papa,CRC/.test(sinPapa), sinPapa.slice(0, 400));
    const papa = toolText(await callTool('bancos_resumen', { mes: '2026-10', de: 'papa' }));
    check('bancos_resumen da solo lo del papá', /movimientos: 1/.test(papa) && papa.includes('CRC,0,8500,-8500,1'), papa.slice(0, 300));
    const bac = toolText(await callTool('bancos_resumen', { mes: '2026-10', cuenta: 'bac', excluirTipos: ['comida'] }));
    check('bancos_resumen filtra por cuenta y excluye tipos', /movimientos: 1/.test(bac), bac.slice(0, 200));
    const nov = toolText(await callTool('bancos_resumen', { mes: '2026-11' }));
    check('bancos_resumen no suma lo pendiente', /movimientos: 1/.test(nov) && !/pendientesSinMovimiento/.test(nov), nov.slice(0, 300));
  },
};
