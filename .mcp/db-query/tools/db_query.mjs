import { queryReadonly } from "../src/database.mjs";
export default {
  name: "db_query", order: 30, format: "json",
  description: "Ejecuta una sola consulta SELECT/CTE con timeout y máximo 500 filas. Devuelve filas sanitizadas y guarda auditoría local; bloquea DDL, DML, procedimientos y columnas sensibles.",
  inputSchema: { type: "object", properties: { sql: { type: "string" }, maxRows: { type: "integer" }, timeoutSeconds: { type: "integer" } }, required: ["sql"], additionalProperties: false },
  handler: (args) => queryReadonly(args),
  async smoke({ callTool, check, toolStructured, toolJson, state }) { const blocked = toolJson(await callTool("db_query", { sql: "DELETE FROM dbo.X" })); check("db_query bloquea escritura", typeof blocked.error === "string" && blocked.error.includes("SELECT")); if (!state.connected) return; const result = toolStructured(await callTool("db_query", { sql: "SELECT 1 AS valor", maxRows: 1 })); check("db_query devuelve SELECT", result.total === 1 && result.rows?.[0]?.valor === 1 && typeof result.resultPath === "string"); },
};
