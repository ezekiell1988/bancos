import { runtime } from "../src/runtime.mjs";
export default {
  name: "ia_create_decision", description: "Crea un ADR individual y actualiza el índice 06_decisions.md. Preview por defecto.",
  inputSchema: { type: "object", properties: { title: { type: "string" }, domain: { type: "string" }, status: { type: "string", enum: ["propuesta", "aceptada", "reemplazada"] }, context: { type: "string" }, decision: { type: "string" }, reason: { type: "string" }, alternatives: { type: "array", items: { type: "string" } }, consequences: { type: "array", items: { type: "string" } }, replaces: { type: "string" }, apply: { type: "boolean" } }, required: ["title", "domain", "context", "decision", "reason"], additionalProperties: false },
  handler: (args) => runtime.write.runWriteOperation("create_decision", args),
  async smoke({ callTool, check, toolJson }) { const result = toolJson(await callTool("ia_create_decision", { title: "Decisión temporal de smoke", domain: "MCP", context: "Se valida el preview de ADR.", decision: "No aplicar cambios reales.", reason: "El smoke debe ser seguro." })); check("ia_create_decision preview", result.applied === false); check("ia_create_decision cubre índice y ADR", result.changes?.length === 2 && result.changes.every((change) => change.path.startsWith("06_decisions"))); },
};
