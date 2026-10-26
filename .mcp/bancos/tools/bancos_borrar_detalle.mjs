import { buscarDetalle, guardarDetalle, guardarDetalles, leerDetalles, sinUbicacion } from '../src/datos.mjs';
import { refrescarSqlite } from '../src/sqlite.mjs';
import { pagosDe } from '../src/vinculos.mjs';

export default {
  name: 'bancos_borrar_detalle',
  order: 42,
  description:
    'Borra un comprobante (c-...) o factura (f-...) por id (sale de bancos_detalles). No toca su movimiento ni el archivo de respaldo; ' +
    'un comprobante borrado sale de los pagos de sus facturas. ' +
    'Sin confirm:true solo muestra lo que se borraría.',
  inputSchema: {
    type: 'object',
    properties: {
      id: { type: 'string', description: 'id del comprobante o factura.' },
      confirm: { type: 'boolean', description: 'true para borrar; sin él solo preview.' },
    },
    required: ['id'],
    additionalProperties: false,
  },
  async handler({ id, confirm }) {
    const d = buscarDetalle(id);
    const facturas = d.nivel === 'comprobantes' ? leerDetalles('facturas').filter((f) => pagosDe(f).includes(d.id)) : [];
    const pagaA = facturas.length ? facturas.map((f) => f.id) : undefined;
    if (confirm !== true) return { preview: true, applied: false, detalle: sinUbicacion(d), pagaA, message: 'Llamar de nuevo con confirm:true para borrarlo.' };
    guardarDetalle(d, null);
    guardarDetalles(facturas.map((f) => {
      const nueva = { ...sinUbicacion(f), comprobantes: pagosDe(f).filter((x) => x !== d.id) };
      if (!nueva.comprobantes.length) delete nueva.comprobantes;
      return { detalle: f, nuevo: nueva };
    }));
    return { applied: true, borrado: sinUbicacion(d), aviso: refrescarSqlite() };
  },
  async smoke({ callTool, check, toolJson, state }) {
    const prev = toolJson(await callTool('bancos_borrar_detalle', { id: state.comprobanteSoda }));
    check('bancos_borrar_detalle hace preview', prev.preview === true && prev.detalle?.comercio === 'Soda X' && prev.pagaA?.includes(state.facturaTaller), JSON.stringify(prev).slice(0, 200));
    const no = toolJson(await callTool('bancos_borrar_detalle', { id: 'c-00000000' }));
    check('bancos_borrar_detalle rechaza id inexistente', /No existe/.test(no.error ?? ''), no.error);
  },
};
