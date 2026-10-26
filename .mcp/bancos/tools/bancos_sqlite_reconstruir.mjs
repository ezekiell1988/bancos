import { reconstruirSqlite } from '../src/sqlite.mjs';

export default {
  name: 'bancos_sqlite_reconstruir',
  order: 51,
  description:
    'Reconstruye datos/bancos.sqlite desde los jsonl (los tools de escritura ya lo hacen solos). Usar si se borró la base ' +
    'o si un jsonl se cambió fuera del MCP. Devuelve cuántas filas cargó de cada tabla.',
  inputSchema: { type: 'object', properties: {}, additionalProperties: false },
  async handler() {
    return { reconstruida: true, ...reconstruirSqlite() };
  },
  async smoke({ callTool, check, toolJson }) {
    const r = toolJson(await callTool('bancos_sqlite_reconstruir', {}));
    check('bancos_sqlite_reconstruir carga las tablas', r.reconstruida === true && r.movimientos === 5 && r.comprobantes === 2 && r.facturas === 2 && r.lineas === 3 && r.pagos === 1, JSON.stringify(r));
  },
};
