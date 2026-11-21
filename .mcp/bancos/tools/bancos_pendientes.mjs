import { CLASES, CUENTAS, emparejar, RUBRO_PROPS, filtroRubro, leerDetalles, leerTodosLosDetalles, leerTodosLosMovimientos, rubroDe } from '../src/datos.mjs';
import { periodoProps, resolverPeriodo } from '../src/periodo.mjs';
import { estadoPago, mapaComprobantes, pagosDe } from '../src/vinculos.mjs';

export default {
  name: 'bancos_pendientes',
  format: 'toon',
  order: 33,
  description:
    'Qué hay que conciliar de un ciclo (19 al 18), mes o rango: solo lo que tiene documento. Devuelve comprobantes o facturas sin movimiento ' +
    '(detallesSinMovimiento: esperan el estado de cuenta o no se encontró su línea; una factura con pagos no cuenta aquí: se concilia por sus comprobantes), ' +
    'facturas con saldo (pagadas en parte por sus comprobantes) y movimientos sin categoría (sinCategoria: tipo «otros» o sin tipo, para ponerles uno). ' +
    'Un movimiento sin comprobante ni factura NO es pendiente: solo necesita su tipo. Filtros: cuenta, clase y rubro. Respuesta TOON.',
  inputSchema: {
    type: 'object',
    properties: {
      ...periodoProps,
      cuenta: { type: 'string', enum: CUENTAS, description: 'Default: todas.' },
      clase: { type: 'string', enum: CLASES },
      ...RUBRO_PROPS,
      ver: { type: 'string', enum: ['ambos', 'detalles', 'categoria'], description: 'detalles = documentos sin movimiento y facturas con saldo; categoria = movimientos sin categoría; ambos (default) = todo.' },
    },
    additionalProperties: false,
  },
  async handler(args) {
    const { desde, hasta } = resolverPeriodo(args);
    const ver = args.ver ?? 'ambos';
    const todos = leerTodosLosDetalles();
    const comprobantes = mapaComprobantes(todos);
    const rubro = filtroRubro(args.de, args.excluirDe);
    const sinCategoria = ver === 'detalles' ? undefined : leerTodosLosMovimientos(desde, hasta, args.cuenta ? [args.cuenta] : CUENTAS)
      .filter((m) => (!m.tipo || m.tipo === 'otros') && rubro(m) && (!args.clase || m.clase === args.clase))
      .map((m) => ({ id: m.id, fecha: m.fecha, cuenta: m.cuenta, clase: m.clase, tipo: m.tipo ?? '', total: m.total, moneda: m.moneda, comercio: m.comercio, rubro: rubroDe(m) }));
    const detallesSinMovimiento = ver === 'categoria' ? undefined : ['comprobantes', 'facturas']
      .flatMap((n) => leerDetalles(n, desde, hasta))
      .filter((d) => !d.movimiento && !pagosDe(d).length && rubro(d) && (!args.cuenta || d.cuenta === args.cuenta) && (!args.clase || (d.clase ?? 'gasto') === args.clase))
      .map((d) => ({ id: d.id, nivel: d.nivel === 'comprobantes' ? 2 : 3, fecha: d.fecha, cuenta: d.cuenta ?? '', total: d.total, moneda: d.moneda, comercio: d.comercio, referencia: d.referencia ?? d.numero ?? '' }));
    const facturasConSaldo = ver === 'categoria' ? undefined : leerDetalles('facturas', desde, hasta)
      .filter((f) => pagosDe(f).length && rubro(f) && (!args.cuenta || f.cuenta === args.cuenta) && (!args.clase || args.clase === 'gasto'))
      .map((f) => ({ id: f.id, fecha: f.fecha, total: f.total, moneda: f.moneda, comercio: f.comercio, pagos: pagosDe(f).length, ...estadoPago(f, comprobantes) }))
      .filter((f) => f.saldo > 1);
    return {
      desde, hasta,
      ...(detallesSinMovimiento ? { detallesSinMovimiento: { total: detallesSinMovimiento.length, lista: detallesSinMovimiento } } : {}),
      ...(facturasConSaldo?.length ? { facturasConSaldo } : {}),
      ...(sinCategoria ? { sinCategoria: { total: sinCategoria.length, lista: sinCategoria } } : {}),
    };
  },
  async smoke({ callTool, check, toolText }) {
    const oct = toolText(await callTool('bancos_pendientes', { mes: '2026-10', ver: 'categoria' }));
    check('bancos_pendientes da los movimientos sin categoría y no los sin papel', /sinCategoria:\s+total: \d+/.test(oct) && !oct.includes('movimientosSinDetalle') && !oct.includes('detallesSinMovimiento'), oct.slice(0, 300));
    const nov = toolText(await callTool('bancos_pendientes', { mes: '2026-11', ver: 'detalles' }));
    check('bancos_pendientes lista facturas con saldo', /facturasConSaldo/.test(nov) && nov.includes('Taller Z') && !nov.includes('sinCategoria'), nov.slice(0, 400));
    const mov = { id: 'm-1', fecha: '2026-11-17', cuenta: 'bac', clase: 'gasto', moneda: 'CRC', total: 15227.53 };
    const factura = { id: 'f-1', nivel: 'facturas', fecha: '2026-11-01', moneda: 'CRC', total: 15227.49 };
    check('emparejar vincula una factura pagada 16 días después con céntimos de diferencia', emparejar([factura], [mov]).length === 1);
    check('emparejar no relaja la ventana ni el monto de un comprobante', emparejar([{ ...factura, id: 'c-1', nivel: 'comprobantes', clase: 'gasto' }], [mov]).length === 0);
  },
};
