import { runtime } from "../src/runtime.mjs";

export default {
  name: "return_task_to_draft",
  order: 35,
  description: "Devuelve una tarea Lista, En progreso o Bloqueada a Borrador con motivo. Preview por defecto.",
  inputSchema: {
    type: "object",
    properties: { id: { type: "string" }, reason: { type: "string" }, apply: { type: "boolean" } },
    required: ["id", "reason"],
    additionalProperties: false,
  },
  handler: (args) => runtime.write.runWriteOperation("return_task_to_draft", args),
  async smoke({ callTool, check, toolJson, state }) {
    const result = toolJson(await callTool("return_task_to_draft", { id: state.taskId, reason: "Requiere revisión humana antes de continuar." }));
    check("return_task_to_draft preview por defecto", result.applied === false);
    const applied = toolJson(await callTool("return_task_to_draft", { id: state.taskId, reason: "Requiere revisión humana antes de continuar.", apply: true }));
    check("return_task_to_draft devuelve a Borrador", applied.applied === true);
    const reapproved = toolJson(await callTool("approve_task", { id: state.taskId, apply: true }));
    check("return_task_to_draft exige nueva aprobación", reapproved.applied === true);
  },
};
