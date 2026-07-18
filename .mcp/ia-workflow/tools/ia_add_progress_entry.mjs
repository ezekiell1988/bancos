import { runtime } from "../src/runtime.mjs";
export default {
  name: "ia_add_progress_entry", description: "Agrega progreso actual y opcionalmente por componente. Preview por defecto.",
  inputSchema: { type: "object", properties: { text: { type: "string" }, component: { type: "string", enum: ["backend", "frontend", "database", "infrastructure", "documentation", "quality"] }, authorInitials: { type: "string" }, apply: { type: "boolean" } }, required: ["text"], additionalProperties: false },
  handler: (args) => runtime.write.runWriteOperation("add_progress_entry", args),
  async smoke({ callTool, check, toolJson }) { const result = toolJson(await callTool("ia_add_progress_entry", { text: "Progreso de prueba sin aplicar.", component: "documentation" })); check("ia_add_progress_entry preview", result.applied === false); check("ia_add_progress_entry cubre current y componente", result.changes?.length === 2); },
};
