import { NIVELES, RUBRO_PROPS, ToolError, filtroRubro, leerDetalles, leerTodosLosDetalles, rubroDe, slug } from '../src/datos.mjs';
import { periodoProps, resolverPeriodo } from '../src/periodo.mjs';
import { estadoPago, mapaComprobantes, movimientosDeFactura, pagosDe } from '../src/vinculos.mjs';

export default {
  name: 'bancos_detalles',
  format: 'toon',
  order: 32,
  description:
    'Lista comprobantes (nivel 2) y facturas (nivel 3) de un mes o rango (por su fecha), o los de un movimiento (movimiento: m-...). ' +
    'Cada uno con su id y el movimiento al que está vinculado (vacío = pendiente; una factura con pagos toma los de sus comprobantes). ' +
    'pagos = comprobantes de la factura (o facturas que paga un comprobante); saldo = total de la factura menos lo pagado. ' +
    'conLineas:true agrega las líneas de las facturas. ' +
    'Filtros: nivel, pendientes:true, rubro (de / excluirDe) y texto. Respuesta TOON.',
  inputSchema: {
    type: 'object',
    properties: {
      ...periodoProps,
      movimiento: { type: 'string', description: 'id del movimiento; si se indica, no hace falta periodo.' },
      nivel: { type: 'string', enum: Object.keys(NIVELES), description: 'Default: ambos.' },
      pendientes: { type: 'boolean', description: 'true = solo los que no tienen movimiento.' },
      ...RUBRO_PROPS,
      texto: { type: 'string', description: 'Busca en comercio, nota, referencia, número y líneas.' },
      conLineas: { type: 'boolean', description: 'true para incluir las líneas de cada factura.' },
    },
    additionalProperties: false,
  },
  async handler(args) {
    const { desde, hasta } = args.movimiento && !args.mes && !args.desde && !args.hasta ? {} : resolverPeriodo(args);
    if (args.movimiento && !/^m-/.test(args.movimiento)) throw new ToolError('movimiento debe ser un id m-...');
    const buscar = args.texto ? slug(args.texto) : null;
    const niveles = args.nivel ? [args.nivel] : Object.keys(NIVELES);
    const todos = leerTodosLosDetalles();
    const comprobantes = mapaComprobantes(todos);
    const movs = (d) => (d.nivel === 'facturas' ? movimientosDeFactura(d, comprobantes) : [d.movimiento].filter(Boolean));
    const pagos = (d) => (d.nivel === 'facturas' ? pagosDe(d) : todos.filter((f) => f.nivel === 'facturas' && pagosDe(f).includes(d.id)).map((f) => f.id));
    const filas = niveles.flatMap((n) => leerDetalles(n, desde, hasta))
      .filter((d) => (!args.movimiento || movs(d).includes(args.movimiento)) && (!args.pendientes || !movs(d).length))
      .filter(filtroRubro(args.de, args.excluirDe))
      .filter((d) => !buscar || slug(`${d.comercio} ${d.nota} ${d.referencia ?? ''} ${d.numero ?? ''} ${(d.lineas ?? []).map((l) => l.descripcion).join(' ')}`).includes(buscar))
      .sort((a, b) => a.fecha.localeCompare(b.fecha) || a.nivel.localeCompare(b.nivel));
    const detalles = filas.map((d) => ({
      id: d.id, nivel: d.nivel === 'comprobantes' ? 2 : 3, fecha: d.fecha, cuenta: d.cuenta ?? '', total: d.total, moneda: d.moneda,
      comercio: d.comercio, referencia: d.referencia ?? d.numero ?? '', tipo: d.tipo ?? '', rubro: rubroDe(d), nota: d.nota,
      lineas: d.lineas?.length ?? 0, movimiento: movs(d).join(' '), pagos: pagos(d).join(' '),
      saldo: d.nivel === 'facturas' ? estadoPago(d, comprobantes).saldo : '', archivo: d.archivo ?? '',
    }));
    const lineas = args.conLineas
      ? filas.filter((d) => d.lineas).flatMap((d) => d.lineas.map((l, i) => ({ factura: d.id, n: i + 1, descripcion: l.descripcion,
        cantidad: l.cantidad ?? '', unidad: l.unidad ?? '', precio: l.precio ?? '', total: l.total, tipo: l.tipo ?? '' })))
      : undefined;
    return { ...(desde ? { desde, hasta } : {}), total: detalles.length, detalles, ...(lineas ? { lineas } : {}) };
  },
  async smoke({ callTool, check, toolText, state }) {
    const oct = toolText(await callTool('bancos_detalles', { mes: '2026-10', conLineas: true }));
    check('bancos_detalles lista comprobantes y facturas con líneas', /total: 2/.test(oct) && oct.includes('Arroz') && oct.includes(state.comprobanteSoda), oct.slice(0, 600));
    const pend = toolText(await callTool('bancos_detalles', { desde: '2026-01-01', pendientes: true }));
    check('bancos_detalles filtra pendientes', /total: 0/.test(pend), pend.slice(0, 200));
    const taller = toolText(await callTool('bancos_detalles', { mes: '2026-11', nivel: 'facturas' }));
    check('bancos_detalles da pagos y saldo de la factura', taller.includes(state.comprobanteSoda) && /,3000,/.test(taller), taller.slice(0, 400));
  },
};
