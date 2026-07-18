import { listTables } from "../src/database.mjs";
export default {
  name: "db_list_tables", order: 10, format: "json",
  description: "Lista tablas y vistas accesibles, con nombres y esquema; guarda auditoría local.",
  inputSchema: { type: "object", properties: {}, additionalProperties: false },
  handler: () => listTables(),
  async smoke({ callTool, check, toolStructured, state }) { if (!state.connected) return; const result = toolStructured(await callTool("db_list_tables")); check("db_list_tables devuelve total", Number.isInteger(result.total)); check("db_list_tables devuelve ruta", typeof result.resultPath === "string"); },
};
