import { CUENTAS, buscarDetalle, buscarMovimiento, detalleSinUbicacion, guardarDetalles, leerDetalles } from '../src/datos.mjs';
import { refrescarSqlite } from '../src/sqlite.mjs';

/** Evalúa un cambio {id, cuenta}: qué se hará con ese comprobante o factura. */
function evaluar({ id, cuenta }) {
  const d = buscarDetalle(id);
  if (d.cuenta === cuenta) return { id, estado: 'ya-la-tiene', cuenta };
  if (d.cuenta) return { id, estado: 'conflicto', motivo: `ya tiene ${d.cuenta}; no se pisa` };
  const mid = d.nivel === 'facturas' ? leerDetalles('libros').find((l) => l.id === d.libro)?.movimiento : d.movimiento;
  const m = mid ? buscarMovimiento(mid) : null;
  if (m && m.cuenta !== cuenta) return { id, estado: 'conflicto', motivo: `su movimiento ${m.id} es de ${m.cuenta}` };
  return { id, estado: 'fijar', cuenta, d };
}

export default {
  name: 'bancos_fijar_cuenta',
  order: 46,
  description:
    'Fija la cuenta (bcr, bac, bncr) de libros (l-...) y facturas (f-...) que no la tienen, en lote de {id, cuenta}. ' +
    'No pisa una cuenta distinta ni contradice la del movimiento vinculado, y no cambia ningún otro dato. ' +
    'Sin apply:true solo muestra el preview. Usar solo con evidencia cierta de la cuenta.',
  inputSchema: {
    type: 'object',
    properties: {
      cambios: {
        type: 'array',
        minItems: 1,
        items: {
          type: 'object',
          properties: {
            id: { type: 'string', description: 'id del comprobante o factura.' },
            cuenta: { type: 'string', enum: CUENTAS },
          },
          required: ['id', 'cuenta'],
          additionalProperties: false,
        },
      },
      apply: { type: 'boolean', description: 'true para guardar; sin él solo preview.' },
    },
    required: ['cambios'],
    additionalProperties: false,
  },
  async handler({ cambios, apply }) {
    const plan = cambios.map((c) => {
      try { return evaluar(c); } catch (e) { return { id: c.id, estado: 'error', motivo: e.message }; }
    });
    const fijar = plan.filter((p) => p.estado === 'fijar');
    const resumen = plan.map(({ d: _d, ...p }) => p);
    const cuenta = (estado) => plan.filter((p) => p.estado === estado).length;
    const conteo = { fijar: fijar.length, yaLaTienen: cuenta('ya-la-tiene'), conflictos: cuenta('conflicto'), errores: cuenta('error') };
    if (apply !== true) return { preview: true, applied: false, ...conteo, cambios: resumen, message: 'Llamar de nuevo con apply:true para guardar.' };
    guardarDetalles(fijar.map((p) => ({ detalle: p.d, nuevo: { ...detalleSinUbicacion(p.d), cuenta: p.cuenta } })));
    return { applied: true, ...conteo, cambios: resumen, aviso: refrescarSqlite() };
  },
  async smoke({ callTool, check, toolJson, state }) {
    const igual = toolJson(await callTool('bancos_fijar_cuenta', { cambios: [{ id: state.libroSoda, cuenta: 'bac' }] }));
    check('bancos_fijar_cuenta: el libro conserva su cuenta', igual.yaLaTienen === 1 && igual.fijar === 0, JSON.stringify(igual).slice(0, 200));
    const otra = toolJson(await callTool('bancos_fijar_cuenta', { cambios: [{ id: state.libroSoda, cuenta: 'bcr' }] }));
    check('bancos_fijar_cuenta no pisa otra cuenta', otra.conflictos === 1, JSON.stringify(otra).slice(0, 200));
  },
};
