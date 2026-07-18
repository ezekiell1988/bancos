import { runtime } from "../src/runtime.mjs";
export default {
  name: "ia_list_tasks", description: "Lista tareas activas, backlog, bloqueadas, completadas o todas en modo compacto.",
  inputSchema: { type: "object", properties: { status: { type: "string", enum: ["active", "backlog", "blocked", "done", "all"] }, mode: { type: "string", enum: ["full", "summary", "pathsOnly"] } }, additionalProperties: false },
  handler: (args) => runtime.read.listTasks(args),
  async smoke({ callTool, check, toolJson }) { const result = toolJson(await callTool("ia_list_tasks", { status: "active", mode: "summary" })); check("ia_list_tasks devuelve grupo activo", result.groups?.active?.path === "04_tasks/current.md"); check("ia_list_tasks devuelve archivos", Array.isArray(result.groups?.active?.files)); },
};
