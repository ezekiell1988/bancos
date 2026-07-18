import { runtime } from "../src/runtime.mjs";
export default {
  name: "ia_read_task", description: "Lee una tarea activa por ID en modo full o summary.",
  inputSchema: { type: "object", properties: { id: { type: "string" }, mode: { type: "string", enum: ["full", "summary"] }, maxChars: { type: "integer" } }, required: ["id"], additionalProperties: false },
  handler: (args) => runtime.read.readTask(args),
  async smoke({ callTool, check, toolJson }) { const tasks = toolJson(await callTool("ia_list_tasks", { status: "active" })); const id = tasks.groups?.active?.files?.[0]?.id; if (!id) return; const result = toolJson(await callTool("ia_read_task", { id, mode: "summary" })); check("ia_read_task conserva ID", result.id === id); check("ia_read_task devuelve summary", Boolean(result.summary)); },
};
