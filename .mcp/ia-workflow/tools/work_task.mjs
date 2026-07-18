import { runtime } from "../src/runtime.mjs";

export default {
  name: "work_task",
  order: 30,
  description: "Valida gates y devuelve contexto para una tarea Lista o En progreso sin editar código.",
  inputSchema: { type: "object", properties: { id: { type: "string" }, mode: { type: "string", enum: ["summary", "full"] }, maxChars: { type: "integer" } }, required: ["id"], additionalProperties: false },
  handler: (args) => runtime.write.workTask(args),
  async smoke({ callTool, check, toolJson, state }) {
    const result = toolJson(await callTool("work_task", { id: state.taskId, mode: "summary" }));
    check("work_task permite tarea Lista", result.allowed === true && result.status === "Lista");
  },
};
