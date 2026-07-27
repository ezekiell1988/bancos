import { execQuery } from "../src/database.mjs";
export default {
  name: "db_exec", order: 35, format: "json",
  description: "Ejecuta T-SQL arbitrario (INSERT, UPDATE, DELETE, DDL, SELECT) y genera un reporte Markdown. Requiere apply:true como confirmación explícita por llamada. Bloquea columnas sensibles en el resultado.",
  inputSchema: {
    type: "object",
    properties: {
      sql: { type: "string", description: "Sentencia T-SQL a ejecutar." },
      apply: { type: "boolean", description: "Debe ser true para ejecutar la operación." },
      timeoutSeconds: { type: "integer", description: "Timeout en segundos (1-60, default 15)." }
    },
    required: ["sql", "apply"],
    additionalProperties: false
  },
  handler: (args) => execQuery(args),
  async smoke({ callTool, check, toolJson }) {
    const blocked = toolJson(await callTool("db_exec", { sql: "SELECT 1", apply: false }));
    check("db_exec bloquea sin apply:true", typeof blocked.error === "string" && blocked.error.includes("apply"));
  },
};
