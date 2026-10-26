import { CLASES, CUENTAS, RUBRO_PROPS, filtroRubro, leerDetalles, leerTodosLosDetalles, leerTodosLosMovimientos, rubroDe } from '../src/datos.mjs';
import { periodoProps, resolverPeriodo } from '../src/periodo.mjs';
import { estadoPago, mapaComprobantes, pagosDe } from '../src/vinculos.mjs';

export default {
  name: 'bancos_pendientes',
  format: 'toon',
  order: 33,
  description:
    'Conciliación de un mes o rango: movimientos bancarios (nivel 1) sin comprobante ni factura, y comprobantes o facturas ' +
    'sin movimiento (esperan el estado de cuenta o no se encontró su línea); una factura con pagos no cuenta aquí: se concilia por sus comprobantes. ' +
    'También las facturas con saldo (pagadas en parte por sus comprobantes). Filtros: cuenta, clase y rubro. Respuesta TOON.',
  inputSchema: {
    type: 'object',
    properties: {
      ...periodoProps,
      cuenta: { type: 'string', enum: CUENTAS, description: 'Default: todas.' },
      clase: { type: 'string', enum: CLASES },
      ...RUBRO_PROPS,
      ver: { type: 'string', enum: ['ambos', 'movimientos', 'detalles'], description: 'Default: ambos.' },
    },
    additionalProperties: false,
  },
  async handler(args) {
    const { desde, hasta } = resolverPeriodo(args);
    const ver = args.ver ?? 'ambos';
    const todos = leerTodosLosDetalles();
    const comprobantes = mapaComprobantes(todos);
    const conDetalle = new Set(todos.filter((d) => d.movimiento).map((d) => d.movimiento));
    const rubro = filtroRubro(args.de, args.excluirDe);
    const movimientosSinDetalle = ver === 'detalles' ? undefined : leerTodosLosMovimientos(desde, hasta, args.cuenta ? [args.cuenta] : CUENTAS)
      .filter((m) => !conDetalle.has(m.id) && rubro(m) && (!args.clase || m.clase === args.clase))
      .map((m) => ({ id: m.id, fecha: m.fecha, cuenta: m.cuenta, clase: m.clase, tipo: m.tipo, total: m.total, moneda: m.moneda, comercio: m.comercio, rubro: rubroDe(m) }));
    const detallesSinMovimiento = ver === 'movimientos' ? undefined : ['comprobantes', 'facturas']
      .flatMap((n) => leerDetalles(n, desde, hasta))
      .filter((d) => !d.movimiento && !pagosDe(d).length && rubro(d) && (!args.cuenta || d.cuenta === args.cuenta) && (!args.clase || (d.clase ?? 'gasto') === args.clase))
      .map((d) => ({ id: d.id, nivel: d.nivel === 'comprobantes' ? 2 : 3, fecha: d.fecha, cuenta: d.cuenta ?? '', total: d.total, moneda: d.moneda, comercio: d.comercio, referencia: d.referencia ?? d.numero ?? '' }));
    const facturasConSaldo = ver === 'movimientos' ? undefined : leerDetalles('facturas', desde, hasta)
      .filter((f) => pagosDe(f).length && rubro(f) && (!args.cuenta || f.cuenta === args.cuenta) && (!args.clase || args.clase === 'gasto'))
      .map((f) => ({ id: f.id, fecha: f.fecha, total: f.total, moneda: f.moneda, comercio: f.comercio, pagos: pagosDe(f).length, ...estadoPago(f, comprobantes) }))
      .filter((f) => f.saldo > 1);
    return {
      desde, hasta,
      ...(movimientosSinDetalle ? { movimientosSinDetalle: { total: movimientosSinDetalle.length, lista: movimientosSinDetalle } } : {}),
      ...(detallesSinMovimiento ? { detallesSinMovimiento: { total: detallesSinMovimiento.length, lista: detallesSinMovimiento } } : {}),
      ...(facturasConSaldo?.length ? { facturasConSaldo } : {}),
    };
  },
  async smoke({ callTool, check, toolText }) {
    const oct = toolText(await callTool('bancos_pendientes', { mes: '2026-10', ver: 'movimientos' }));
    check('bancos_pendientes da los movimientos sin detalle', /movimientosSinDetalle:\s+total: 3/.test(oct) && !oct.includes('detallesSinMovimiento'), oct.slice(0, 300));
    const nov = toolText(await callTool('bancos_pendientes', { mes: '2026-11', ver: 'detalles' }));
    check('bancos_pendientes lista facturas con saldo', /facturasConSaldo/.test(nov) && nov.includes('Taller Z'), nov.slice(0, 400));
  },
};
