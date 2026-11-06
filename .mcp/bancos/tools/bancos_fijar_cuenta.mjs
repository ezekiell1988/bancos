import { CUENTAS, buscarDetalle, buscarMovimiento, detalleSinUbicacion, guardarDetalles } from '../src/datos.mjs';
import { refrescarSqlite } from '../src/sqlite.mjs';

/** Evalúa un cambio {id, cuenta}: qué se hará con ese comprobante o factura. */
function evaluar({ id, cuenta }) {
  const d = buscarDetalle(id);
  if (d.cuenta === cuenta) return { id, estado: 'ya-la-tiene', cuenta };
  if (d.cuenta) return { id, estado: 'conflicto', motivo: `ya tiene ${d.cuenta}; no se pisa` };
  const m = d.movimiento ? buscarMovimiento(d.movimiento) : null;
  if (m && m.cuenta !== cuenta) return { id, estado: 'conflicto', motivo: `su movimiento ${m.id} es de ${m.cuenta}` };
  return { id, estado: 'fijar', cuenta, d };
}

export default {
  name: 'bancos_fijar_cuenta',
  order: 46,
  description:
    'Fija la cuenta (bcr, bac, bncr) de comprobantes (c-...) y facturas (f-...) que no la tienen, en lote de {id, cuenta}. ' +
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
    const igual = toolJson(await callTool('bancos_fijar_cuenta', { cambios: [{ id: state.comprobanteSoda, cuenta: 'bac' }] }));
    check('bancos_fijar_cuenta: el comprobante conserva su cuenta tras vincular y desvincular', igual.yaLaTienen === 1 && igual.fijar === 0, JSON.stringify(igual).slice(0, 200));
    const otra = toolJson(await callTool('bancos_fijar_cuenta', { cambios: [{ id: state.comprobanteSoda, cuenta: 'bcr' }] }));
    check('bancos_fijar_cuenta no pisa una cuenta distinta', otra.conflictos === 1 && otra.fijar === 0, JSON.stringify(otra).slice(0, 200));
    const sin = toolJson(await callTool('bancos_fijar_cuenta', { cambios: [{ id: state.comprobantePendiente, cuenta: 'bcr' }] }));
    check('bancos_fijar_cuenta hace preview de un detalle sin cuenta', sin.preview === true && (sin.fijar === 1 || sin.yaLaTienen === 1), JSON.stringify(sin).slice(0, 200));
    const malo = toolJson(await callTool('bancos_fijar_cuenta', { cambios: [{ id: 'c-00000000', cuenta: 'bcr' }] }));
    check('bancos_fijar_cuenta reporta un id inexistente', malo.errores === 1, JSON.stringify(malo).slice(0, 200));
  },
};
