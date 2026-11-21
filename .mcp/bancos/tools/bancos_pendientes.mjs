import { CLASES, CUENTAS, RUBRO_PROPS, VENTANA_FACTURA, emparejar, filtroRubro, leerDetalles, leerTodosLosMovimientos, rubroDe } from '../src/datos.mjs';
import { periodoProps, resolverPeriodo } from '../src/periodo.mjs';
import { facturasPorLibro, problemasDeCuadre } from '../src/vinculos.mjs';

export default {
  name: 'bancos_pendientes',
  format: 'toon',
  order: 33,
  description:
    'Qué hay que conciliar de un ciclo (19 al 18), mes o rango (ADR-EBC-03): ' +
    'libroSinMovimiento = pagos (nivel 2) sin movimiento del banco (esperan el estado de cuenta o no se encontró su línea; los en tránsito no cuentan, salen en enTransito); ' +
    'sinCategoria = movimientos sin libro o con alguna partida en «otros» (para ponerles categoría con bancos_editar_movimiento); ' +
    'noCuadran = libros cuyas sumas no dan (facturas contra el monto del pago, partidas contra las líneas, libro contra movimiento). ' +
    'Un movimiento sin documento NO es pendiente por eso: solo necesita su categoría. Filtros: cuenta, clase y rubro. Respuesta TOON.',
  inputSchema: {
    type: 'object',
    properties: {
      ...periodoProps,
      cuenta: { type: 'string', enum: CUENTAS, description: 'Default: todas.' },
      clase: { type: 'string', enum: CLASES },
      ...RUBRO_PROPS,
      ver: { type: 'string', enum: ['todo', 'sinMovimiento', 'categoria', 'cuadre'], description: 'sinMovimiento = libros sin movimiento; categoria = movimientos sin categoría o en otros; cuadre = sumas que no cuadran; todo (default).' },
    },
    additionalProperties: false,
  },
  async handler(args) {
    const { desde, hasta } = resolverPeriodo(args);
    const ver = args.ver ?? 'todo';
    const rubro = filtroRubro(args.de, args.excluirDe);
    const movimientos = leerTodosLosMovimientos(desde, hasta, args.cuenta ? [args.cuenta] : CUENTAS).filter((m) => rubro(m) && (!args.clase || m.clase === args.clase));
    const libros = leerDetalles('libros', desde, hasta).filter((l) => rubro(l) && (!args.cuenta || l.cuenta === args.cuenta) && (!args.clase || l.clase === args.clase));
    const facturas = facturasPorLibro();
    const fila = (l) => ({ id: l.id, fecha: l.fecha, cuenta: l.cuenta ?? '', clase: l.clase, total: l.total, moneda: l.moneda, comercio: l.comercio, referencia: l.referencia ?? '' });

    const sinMov = libros.filter((l) => !l.movimiento && !l.transito).map(fila);
    const transito = libros.filter((l) => !l.movimiento && l.transito).map((l) => ({ ...fila(l), razon: l.transito.razon }));
    const sinCategoria = movimientos.filter((m) => !m.libro || m.partidas.some((p) => p.tipo === 'otros'))
      .map((m) => ({ id: m.id, fecha: m.fecha, cuenta: m.cuenta, clase: m.clase, total: m.total, moneda: m.moneda, comercio: m.comercio, rubro: rubroDe(m), libro: m.libro ?? '', enOtros: m.partidas?.filter((p) => p.tipo === 'otros').reduce((s, p) => s + p.monto, 0) ?? m.total }));
    const porId = new Map(movimientos.map((m) => [m.id, m]));
    const todosLibros = leerDetalles('libros');
    const candidatos = todosLibros.filter((l) => (l.movimiento ? porId.has(l.movimiento) : libros.some((x) => x.id === l.id)));
    const noCuadran = candidatos
      .map((l) => ({ l, problemas: problemasDeCuadre(l, facturas.get(l.id) ?? [], l.movimiento ? porId.get(l.movimiento) ?? null : null) }))
      .filter((x) => x.problemas.length)
      .map(({ l, problemas }) => ({ ...fila(l), movimiento: l.movimiento ?? '', problemas: problemas.join('; ') }));

    return {
      desde, hasta,
      ...(ver === 'todo' || ver === 'sinMovimiento' ? { libroSinMovimiento: { total: sinMov.length, lista: sinMov } } : {}),
      ...((ver === 'todo' || ver === 'sinMovimiento') && transito.length ? { enTransito: { total: transito.length, lista: transito } } : {}),
      ...(ver === 'todo' || ver === 'categoria' ? { sinCategoria: { total: sinCategoria.length, lista: sinCategoria } } : {}),
      ...(ver === 'todo' || ver === 'cuadre' ? { noCuadran: { total: noCuadran.length, lista: noCuadran } } : {}),
    };
  },
  async smoke({ callTool, check, toolText }) {
    const oct = toolText(await callTool('bancos_pendientes', { mes: '2026-10', ver: 'categoria' }));
    check('bancos_pendientes da los movimientos en «otros» y no pide documento', /sinCategoria:\s+total: 0/.test(oct) && !oct.includes('libroSinMovimiento'), oct.slice(0, 300));
    const nov = toolText(await callTool('bancos_pendientes', { mes: '2026-11' }));
    check('bancos_pendientes lista el nivel 2 en tránsito aparte y no como pendiente', /enTransito/.test(nov) && nov.includes('Bus') && /libroSinMovimiento:\s+total: 0/.test(nov), nov.slice(0, 500));
    check('bancos_pendientes: lo que cuadra no sale en noCuadran', /noCuadran:\s+total: 0/.test(nov), nov.slice(0, 500));
    const mov = { id: 'm-1', fecha: '2026-11-17', cuenta: 'bac', clase: 'gasto', moneda: 'CRC', total: 15227.53 };
    const factura = { id: 'f-1', nivel: 'facturas', fecha: '2026-11-01', moneda: 'CRC', total: 15227.49 };
    check('emparejar vincula una factura pagada 16 días después con céntimos de diferencia', emparejar([factura], [mov], new Set(), VENTANA_FACTURA).length === 1);
    check('emparejar no relaja la ventana ni el monto de un libro', emparejar([{ ...factura, id: 'l-1', nivel: 'libros', clase: 'gasto' }], [mov]).length === 0);
  },
};
