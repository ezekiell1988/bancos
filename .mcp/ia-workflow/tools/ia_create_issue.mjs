import { runtime } from "../src/runtime.mjs";
export default {
  name: "ia_create_issue", order: 50, description: "Crea issue abierto y actualiza 07_issues/current.md. Preview por defecto.",
  inputSchema: issueSchema(), handler: (args) => runtime.write.runWriteOperation("create_issue", args),
  async smoke({ callTool, check, toolJson }) { const result = toolJson(await callTool("ia_create_issue", { title: "Issue de preview", severity: "low", component: "MCP", symptom: "Se prueba creación sin aplicar." })); check("ia_create_issue preview", result.applied === false); check("ia_create_issue cubre 07", result.changes?.every((change) => change.path.startsWith("07_issues/"))); },
};
export function issueSchema() { return { type: "object", properties: { title: { type: "string" }, severity: { type: "string", enum: ["critical", "high", "medium", "low"] }, component: { type: "string" }, symptom: { type: "string" }, rootCause: { type: "string" }, workaround: { type: "string" }, proposedFix: { type: "string" }, linkedTasks: { type: "array", items: { type: "string" } }, authorName: { type: "string" }, authorEmail: { type: "string" }, apply: { type: "boolean" } }, required: ["title", "severity", "component", "symptom"], additionalProperties: false }; }
