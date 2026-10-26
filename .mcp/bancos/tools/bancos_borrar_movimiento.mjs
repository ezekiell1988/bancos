import { CUENTAS, ToolError, checkFecha, diaFile, readJsonl, writeJsonl } from '../src/datos.mjs';

export default {
  name: 'bancos_borrar_movimiento',
  order: 40,
  description:
    'Borra una línea de datos/<cuenta>/AAAA-MM/AAAA-MM-DD.jsonl (la cuenta, fecha y línea salen de bancos_movimientos). ' +
    'Sin confirm:true solo muestra el movimiento que se borraría. Para corregir: borrar y volver a anotar con bancos_registrar. ' +
    'No borra el archivo de respaldo.',
  inputSchema: {
    type: 'object',
    properties: {
      cuenta: { type: 'string', enum: CUENTAS },
      fecha: { type: 'string', description: 'AAAA-MM-DD.' },
      linea: { type: 'integer', minimum: 1, description: 'Número de línea (desde 1).' },
      confirm: { type: 'boolean', description: 'true para borrar; sin él solo preview.' },
    },
    required: ['cuenta', 'fecha', 'linea'],
    additionalProperties: false,
  },
  async handler({ cuenta, fecha, linea, confirm }) {
    checkFecha(fecha);
    const file = diaFile(cuenta, fecha);
    const rows = readJsonl(file);
    const movimiento = rows[linea - 1];
    if (!movimiento) throw new ToolError(`No hay línea ${linea} en ${cuenta} ${fecha} (tiene ${rows.length}).`);
    if (confirm !== true) return { preview: true, applied: false, cuenta, linea, movimiento, message: 'Llamar de nuevo con confirm:true para borrarlo.' };
    rows.splice(linea - 1, 1);
    writeJsonl(file, rows);
    return { applied: true, cuenta, linea, borrado: movimiento, quedan: rows.length };
  },
  async smoke({ callTool, check, toolJson, toolText }) {
    const prev = toolJson(await callTool('bancos_borrar_movimiento', { cuenta: 'bac', fecha: '2026-10-25', linea: 1 }));
    check('bancos_borrar_movimiento hace preview sin confirm', prev.preview === true && prev.movimiento?.comercio === 'Súper Y', JSON.stringify(prev).slice(0, 160));
    const ok = toolJson(await callTool('bancos_borrar_movimiento', { cuenta: 'bac', fecha: '2026-10-25', linea: 1, confirm: true }));
    check('bancos_borrar_movimiento borra con confirm', ok.applied === true && ok.quedan === 0, JSON.stringify(ok).slice(0, 160));
    const text = toolText(await callTool('bancos_movimientos', { mes: '2026-10' }));
    check('el movimiento borrado ya no aparece', /total: 2/.test(text), text.slice(0, 80));
    const no = toolJson(await callTool('bancos_borrar_movimiento', { cuenta: 'bac', fecha: '2026-10-25', linea: 5 }));
    check('bancos_borrar_movimiento rechaza línea inexistente', /No hay línea/.test(no.error ?? ''), no.error);
  },
};
