import { runtime } from "../src/runtime.mjs";

export default {
  name: "approve_task",
  order: 20,
  description: "Valida una tarea Borrador y la mueve a Lista. Preview por defecto.",
  inputSchema: { type: "object", properties: { id: { type: "string" }, approver: { type: "string" }, apply: { type: "boolean" } }, required: ["id"], additionalProperties: false },
  handler: (args) => runtime.write.runWriteOperation("approve_task", args),
  async smoke({ callTool, check, toolJson, state }) {
    const preview = toolJson(await callTool("approve_task", { id: state.taskId }));
    check("approve_task genera preview", preview.applied === false);
    const applied = toolJson(await callTool("approve_task", { id: state.taskId, approver: "smoke", apply: true }));
    check("approve_task mueve a Lista", applied.applied === true);
  },
};
