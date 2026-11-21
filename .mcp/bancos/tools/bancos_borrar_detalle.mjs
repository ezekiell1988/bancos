import { ToolError, buscarDetalle, detalleSinUbicacion, guardarDetalle, leerDetalles } from '../src/datos.mjs';
import { refrescarSqlite } from '../src/sqlite.mjs';

export default {
  name: 'bancos_borrar_detalle',
  order: 42,
  description:
    'Borra un libro (l-..., nivel 2) o una factura (f-..., nivel 3) por id (sale de bancos_detalles). No toca su movimiento ni el archivo de respaldo. ' +
    'Un libro con facturas no se borra solo: conFacturas:true borra también sus facturas (el nivel 3 no existe sin nivel 2). ' +
    'Borrar un libro deja a su movimiento sin categoría (pendiente). Borrar una factura no cambia las partidas del libro: revisarlas con bancos_pendientes (cuadre). ' +
    'Sin confirm:true solo muestra lo que se borraría.',
  inputSchema: {
    type: 'object',
    properties: {
      id: { type: 'string', description: 'id del libro o factura.' },
      conFacturas: { type: 'boolean', description: 'Solo libros: true para borrar también sus facturas.' },
      confirm: { type: 'boolean', description: 'true para borrar; sin él solo preview.' },
    },
    required: ['id'],
    additionalProperties: false,
  },
  async handler({ id, conFacturas, confirm }) {
    const d = buscarDetalle(id);
    const facturas = d.nivel === 'libros' ? leerDetalles('facturas').filter((f) => f.libro === d.id) : [];
    if (facturas.length && !conFacturas) throw new ToolError(`El libro ${d.id} tiene ${facturas.length} factura(s) (${facturas.map((f) => f.id).join(', ')}): conFacturas:true las borra también.`);
    if (confirm !== true) return { preview: true, applied: false, detalle: detalleSinUbicacion(d), borraTambien: facturas.length ? facturas.map((f) => f.id) : undefined, message: 'Llamar de nuevo con confirm:true para borrarlo.' };
    for (const f of facturas) guardarDetalle(leerDetalles('facturas', f.fecha, f.fecha).find((x) => x.id === f.id), null);
    guardarDetalle(d, null);
    return { applied: true, borrado: detalleSinUbicacion(d), facturasBorradas: facturas.length || undefined, aviso: refrescarSqlite() };
  },
  async smoke({ callTool, check, toolJson, state }) {
    const sin = toolJson(await callTool('bancos_borrar_detalle', { id: state.libroSuper }));
    check('bancos_borrar_detalle no borra un libro con facturas sin conFacturas', /conFacturas/.test(sin.error ?? ''), sin.error);
    const prev = toolJson(await callTool('bancos_borrar_detalle', { id: state.libroSuper, conFacturas: true }));
    check('bancos_borrar_detalle hace preview con sus facturas', prev.preview === true && prev.borraTambien?.includes(state.factura), JSON.stringify(prev).slice(0, 200));
    const no = toolJson(await callTool('bancos_borrar_detalle', { id: 'l-00000000' }));
    check('bancos_borrar_detalle rechaza id inexistente', /No existe/.test(no.error ?? ''), no.error);
  },
};
