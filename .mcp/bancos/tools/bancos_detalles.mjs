import { NIVELES, RUBRO_PROPS, ToolError, filtroRubro, leerDetalles, rubroDe, slug } from '../src/datos.mjs';
import { periodoProps, resolverPeriodo } from '../src/periodo.mjs';
import { facturasPorLibro, indexarLibros, problemasDeCuadre } from '../src/vinculos.mjs';

const partidasTexto = (l) => (l.partidas ?? []).map((p) => `${p.tipo}:${p.monto}`).join(' ');

export default {
  name: 'bancos_detalles',
  format: 'toon',
  order: 32,
  description:
    'Lista libros (nivel 2, los pagos con su categoría en partidas) y facturas (nivel 3, cuentas por pagar) de un ciclo (19 al 18), mes o rango (por su fecha), ' +
    'o los de un movimiento (movimiento: m-...). Los libros sin documento ni facturas (solo categoría) se ocultan salvo incluirSinDocumento:true. Cada libro con su movimiento (vacío = por conciliar; transito = razón si está en tránsito), sus partidas y sus facturas; ' +
    'cada factura con el libro del que cuelga. cuadre = problemas de sumas (vacío = cuadra). conLineas:true agrega las líneas de las facturas (con su tipo e impuesto). ' +
    'Filtros: nivel (libros o facturas), pendientes:true (libros sin movimiento y no en tránsito), rubro (de / excluirDe) y texto. Respuesta TOON.',
  inputSchema: {
    type: 'object',
    properties: {
      ...periodoProps,
      movimiento: { type: 'string', description: 'id del movimiento; si se indica, no hace falta periodo.' },
      nivel: { type: 'string', enum: Object.keys(NIVELES), description: 'Default: ambos.' },
      pendientes: { type: 'boolean', description: 'true = solo los libros sin movimiento que no están en tránsito.' },
      ...RUBRO_PROPS,
      texto: { type: 'string', description: 'Busca en comercio, proveedor, nota, referencia, número y líneas.' },
      conLineas: { type: 'boolean', description: 'true para incluir las líneas de cada factura.' },
      incluirSinDocumento: { type: 'boolean', description: 'true para listar también los libros sin documento ni facturas (solo guardan la categoría de un movimiento). Default: se ocultan.' },
    },
    additionalProperties: false,
  },
  async handler(args) {
    const { desde, hasta } = args.movimiento && !args.ciclo && !args.mes && !args.desde && !args.hasta ? {} : resolverPeriodo(args);
    if (args.movimiento && !/^m-/.test(args.movimiento)) throw new ToolError('movimiento debe ser un id m-...');
    const buscar = args.texto ? slug(args.texto) : null;
    const niveles = args.nivel ? [args.nivel] : Object.keys(NIVELES);
    const todosLibros = leerDetalles('libros');
    const { porId } = indexarLibros(todosLibros);
    const facts = facturasPorLibro();
    const movDe = (d) => (d.nivel === 'libros' ? d.movimiento : porId.get(d.libro)?.movimiento) ?? '';
    const filas = niveles.flatMap((n) => leerDetalles(n, desde, hasta))
      .filter((d) => (!args.movimiento || movDe(d) === args.movimiento) && (!args.pendientes || (d.nivel === 'libros' && !d.movimiento && !d.transito)))
      .filter((d) => args.incluirSinDocumento || args.movimiento || d.nivel !== 'libros' || d.documento || !d.movimiento || facts.has(d.id))
      .filter(filtroRubro(args.de, args.excluirDe))
      .filter((d) => !buscar || slug(`${d.comercio ?? d.proveedor} ${d.nota} ${d.referencia ?? ''} ${d.numero ?? ''} ${(d.lineas ?? []).map((l) => l.descripcion).join(' ')}`).includes(buscar))
      .sort((a, b) => a.fecha.localeCompare(b.fecha) || a.nivel.localeCompare(b.nivel));
    const detalles = filas.map((d) => ({
      id: d.id, nivel: d.nivel === 'libros' ? 2 : 3, fecha: d.fecha, cuenta: d.cuenta ?? '', clase: d.clase ?? '', total: d.total, moneda: d.moneda,
      comercio: d.comercio ?? d.proveedor, referencia: d.referencia ?? d.numero ?? '', cedula: d.cedula ?? '', rubro: rubroDe(d), nota: d.nota,
      partidas: d.nivel === 'libros' ? partidasTexto(d) : '',
      lineas: d.lineas?.length ?? 0,
      movimiento: movDe(d), libro: d.nivel === 'libros' ? '' : d.libro,
      facturas: d.nivel === 'libros' ? (facts.get(d.id) ?? []).map((f) => f.id).join(' ') : '',
      transito: d.transito?.razon ?? '',
      cuadre: d.nivel === 'libros' ? problemasDeCuadre(d, facts.get(d.id) ?? [], null).join('; ') : '',
      archivo: d.documento ?? d.archivo ?? '',
    }));
    const lineas = args.conLineas
      ? filas.filter((d) => d.lineas).flatMap((d) => d.lineas.map((l, i) => ({ factura: d.id, n: i + 1, descripcion: l.descripcion,
        cantidad: l.cantidad ?? '', unidad: l.unidad ?? '', precio: l.precio ?? '', impuesto: l.impuesto ?? '', total: l.total, tipo: l.tipo ?? '' })))
      : undefined;
    return { ...(desde ? { desde, hasta } : {}), total: detalles.length, detalles, ...(lineas ? { lineas } : {}) };
  },
  async smoke({ callTool, check, toolText, state }) {
    const oct = toolText(await callTool('bancos_detalles', { mes: '2026-10', conLineas: true }));
    check('bancos_detalles lista libros y facturas con líneas', /total: 3/.test(oct) && oct.includes('Arroz') && oct.includes(state.libroSoda) && oct.includes('3-101-123456'), oct.slice(0, 700));
    const pend = toolText(await callTool('bancos_detalles', { desde: '2026-01-01', pendientes: true }));
    check('bancos_detalles filtra los libros sin movimiento y no cuenta los en tránsito', /total: 0/.test(pend) && !pend.includes('Bus'), pend.slice(0, 200));
    const nov = toolText(await callTool('bancos_detalles', { mes: '2026-11', nivel: 'facturas' }));
    check('bancos_detalles da el libro de cada factura', nov.includes(state.libroRetiro) && /total: 2/.test(nov), nov.slice(0, 400));
    const retiro = toolText(await callTool('bancos_detalles', { movimiento: state.idRetiro }));
    check('bancos_detalles por movimiento trae su libro y las 2 facturas', /total: 3/.test(retiro), retiro.slice(0, 400));
  },
};
