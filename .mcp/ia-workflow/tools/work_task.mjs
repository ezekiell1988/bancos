import { runtime } from "../src/runtime.mjs";

export default {
  name: "work_task",
  order: 30,
  description: "Valida gates y devuelve contexto para una tarea Lista o En progreso; puede iniciar, bloquear o reanudar con preview por defecto.",
  inputSchema: {
    type: "object",
    properties: {
      id: { type: "string" },
      mode: { type: "string", enum: ["summary", "full"] },
      transition: { type: "string", enum: ["start", "blocked", "resumed"] },
      reason: { type: "string", minLength: 1 },
      maxChars: { type: "integer", minimum: 1000, maximum: 50000 },
      apply: { type: "boolean" },
    },
    required: ["id"],
    additionalProperties: false,
  },
  handler: (args) => runtime.write.workTask(args),
  async smoke({ callTool, check, toolJson, state }) {
    const result = toolJson(await callTool("work_task", { id: state.taskId, mode: "summary" }));
    check("work_task permite tarea Lista", result.allowed === true && result.status === "Lista");
    const startPreview = toolJson(await callTool("work_task", { id: state.taskId, transition: "start" }));
    check("work_task start usa preview", startPreview.transition?.applied === false);
    const started = toolJson(await callTool("work_task", { id: state.taskId, transition: "start", apply: true }));
    check("work_task inicia tarea", started.status === "En progreso" && started.transition?.applied === true);
    const blocked = toolJson(await callTool("work_task", { id: state.taskId, transition: "blocked", reason: "Dependencia externa pendiente.", apply: true }));
    check("work_task bloquea tarea", blocked.status === "Bloqueada" && blocked.transition?.applied === true);
    const resumed = toolJson(await callTool("work_task", { id: state.taskId, transition: "resumed", reason: "Dependencia externa resuelta.", apply: true }));
    check("work_task reanuda tarea", resumed.status === "En progreso" && resumed.transition?.applied === true);
  },
};
