import { resetSchemas } from "../src/database.mjs";
export default {
  name: "db_reset_schemas", order: 90, format: "json",
  description: "Elimina TODAS las tablas del schema dbo y elimina completamente el schema HangFire (tablas + schema). Requiere confirmación explícita con confirm: true. Usar solo para regenerar migraciones de EF Core desde cero.",
  inputSchema: { type: "object", properties: { confirm: { type: "boolean", description: "Debe ser true para ejecutar la operación destructiva." } }, required: ["confirm"], additionalProperties: false },
  handler: (args) => resetSchemas(args),
  async smoke({ callTool, check, toolJson }) {
    const blocked = toolJson(await callTool("db_reset_schemas", { confirm: false }));
    check("db_reset_schemas bloquea sin confirm", typeof blocked.error === "string");
  },
};
