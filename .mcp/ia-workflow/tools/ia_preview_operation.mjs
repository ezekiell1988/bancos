import { runtime } from "../src/runtime.mjs";
export default {
  name: "ia_preview_operation", description: "Previsualiza una operación segura de workflow sin aplicar cambios.",
  inputSchema: { type: "object", properties: { operation: { type: "string", enum: ["create_task", "migrate_task", "approve_task", "finish_task", "close_task", "add_progress_entry", "create_issue", "close_issue", "create_decision"] }, arguments: { type: "object" } }, required: ["operation", "arguments"], additionalProperties: false },
  handler: (args) => runtime.write.previewOperation(args),
  async smoke({ callTool, check, toolJson }) { const result = toolJson(await callTool("ia_preview_operation", { operation: "add_progress_entry", arguments: { text: "Validación de preview sin escritura." } })); check("ia_preview_operation no aplica", result.applied === false); check("ia_preview_operation describe cambios", result.changes?.some((change) => change.path === "05_progress/current.md")); },
};
