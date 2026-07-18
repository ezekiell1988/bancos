import { runtime } from "../src/runtime.mjs";
import { taskSchema } from "./create_task.mjs";
export default {
  name: "ia_create_task", description: "Primitiva interna para crear una tarea Borrador y actualizar 04_tasks/current.md. Preview por defecto.",
  inputSchema: taskSchema(), handler: (args) => runtime.write.runWriteOperation("create_task", args),
  async smoke({ callTool, check, toolJson }) { const result = toolJson(await callTool("ia_create_task", { title: "Previsualizar tarea interna", area: "MCP", context: "Se valida la primitiva interna sin aplicar cambios.", objective: "Confirmar preview seguro.", allowedScope: ["Solo preview."], outOfScope: ["Escritura real."], acceptanceCriteria: ["No modifica archivos."], technicalPlan: ["Generar preview."], steps: ["Llamar tool."], expectedOutput: "Preview válido.", validation: ["Smoke."], rollback: "No aplica." })); check("ia_create_task preview", result.applied === false); check("ia_create_task apunta a 04", result.changes?.some((change) => change.path.startsWith("04_tasks/"))); },
};
