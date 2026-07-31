export { sql } from "./sql.mjs";

export const profile = Object.freeze({
  serverName: "db-query-mcp",
  secretsFile: "db.json",
  reportDirectory: ".mcp/db-query/reports/",
  reportTitle: "Ejecución T-SQL - EvistaDev",
  description: "Ejecuta bloques T-SQL ordenados contra EvistaDev y reemplaza un reporte Markdown estructurado.",
  requireApply: false,
  maxTimeoutSeconds: 120,
  defaultTimeoutSeconds: 30,
});