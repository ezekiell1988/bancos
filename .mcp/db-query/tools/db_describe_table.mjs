import { describeTable } from "../src/database.mjs";
export default {
  name: "db_describe_table", order: 20, format: "json",
  description: "Describe columnas de una tabla accesible y devuelve resultados sanitizados con auditoría local.",
  inputSchema: { type: "object", properties: { schema: { type: "string" }, table: { type: "string" } }, required: ["table"], additionalProperties: false },
  handler: (args) => describeTable(args),
  async smoke({ callTool, check, toolJson, state }) { const invalid = toolJson(await callTool("db_describe_table", { table: "../x" })); check("db_describe_table rechaza identificador", typeof invalid.error === "string"); if (!state.connected) return; },
};
