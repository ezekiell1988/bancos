import { CUENTAS, detalleSinUbicacion, guardarDetalle, guardarMovimiento, leerDetalles, sinUbicacion, ubicarMovimiento } from '../src/datos.mjs';
import { bloquea, facturasPorLibro } from '../src/vinculos.mjs';
import { refrescarSqlite } from '../src/sqlite.mjs';

export default {
  name: 'bancos_borrar_movimiento',
  order: 40,
  description:
    'Borra un movimiento bancario (nivel 1) por id, o por cuenta + fecha + línea (salen de bancos_movimientos). ' +
    'Sin confirm:true solo muestra el movimiento que se borraría. Su libro (nivel 2) sin documento ni facturas se borra con él; si tiene documento o facturas no se borra: queda sin movimiento (por conciliar). ' +
    'Para corregir fecha, total o clase: borrar y volver a anotar con bancos_registrar. No borra el archivo de respaldo.',
  inputSchema: {
    type: 'object',
    properties: {
      id: { type: 'string', description: 'id del movimiento (m-...). Alternativa a cuenta + fecha + linea.' },
      cuenta: { type: 'string', enum: CUENTAS },
      fecha: { type: 'string', description: 'AAAA-MM-DD.' },
      linea: { type: 'integer', minimum: 1, description: 'Número de línea (desde 1).' },
      confirm: { type: 'boolean', description: 'true para borrar; sin él solo preview.' },
    },
    additionalProperties: false,
  },
  async handler(args) {
    const m = ubicarMovimiento(args);
    const libros = leerDetalles('libros').filter((d) => m.id && d.movimiento === m.id);
    const facturas = facturasPorLibro();
    const quedanPendientes = libros.filter((l) => bloquea(l, facturas)).map((l) => l.id);
    if (args.confirm !== true) return { preview: true, applied: false, cuenta: m.cuenta, linea: m.linea, movimiento: sinUbicacion(m), quedanPendientes, message: 'Llamar de nuevo con confirm:true para borrarlo.' };
    guardarMovimiento(m, null);
    for (const l of libros) guardarDetalle(l, bloquea(l, facturas) ? { ...detalleSinUbicacion(l), movimiento: null } : null);
    return { applied: true, cuenta: m.cuenta, linea: m.linea, borrado: sinUbicacion(m), quedanPendientes, aviso: refrescarSqlite() };
  },
  async smoke({ callTool, check, toolJson, toolText, state }) {
    // El Súper Y tiene factura: su libro no se borra con él, queda sin movimiento.
    const lista = toolText(await callTool('bancos_movimientos', { mes: '2026-10', texto: 'Súper Y' }));
    state.idSuper = lista.match(/m-[0-9a-f]{8}/)?.[0];
    const prev = toolJson(await callTool('bancos_borrar_movimiento', { id: state.idSuper }));
    check('bancos_borrar_movimiento hace preview y avisa qué libro queda sin movimiento', prev.preview === true && prev.movimiento?.comercio === 'Súper Y' && prev.quedanPendientes?.[0] === state.libroSuper, JSON.stringify(prev).slice(0, 200));
    const ok = toolJson(await callTool('bancos_borrar_movimiento', { cuenta: 'bac', fecha: '2026-10-25', linea: 1, confirm: true }));
    check('bancos_borrar_movimiento borra con confirm', ok.applied === true && ok.borrado?.id === state.idSuper, JSON.stringify(ok).slice(0, 200));
    const pend = toolText(await callTool('bancos_detalles', { mes: '2026-10', pendientes: true }));
    check('el libro con factura del movimiento borrado queda por conciliar', /total: 1/.test(pend) && pend.includes(state.libroSuper), pend.slice(0, 200));
    const no = toolJson(await callTool('bancos_borrar_movimiento', { cuenta: 'bac', fecha: '2026-10-25', linea: 5 }));
    check('bancos_borrar_movimiento rechaza línea inexistente', /No hay línea/.test(no.error ?? ''), no.error);
  },
};
