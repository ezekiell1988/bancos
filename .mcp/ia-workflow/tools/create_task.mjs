import fs from "node:fs/promises";
import path from "node:path";
import { runtime } from "../src/runtime.mjs";

export default {
  name: "create_task",
  order: 10,
  description: "Crea una tarea completa en Borrador. Preview por defecto; aplica solo con apply=true.",
  inputSchema: taskSchema(),
  handler: (args) => runtime.write.runWriteOperation("create_task", args),
  async smoke({ callTool, check, toolJson, state }) {
    const args = fixture();
    const preview = toolJson(await callTool("create_task", args));
    check("create_task preview no muta", preview.applied === false && preview.validation?.valid === true);
    const applied = toolJson(await callTool("create_task", { ...args, apply: true }));
    const taskPath = applied.changes?.find((change) => change.path.includes("04_tasks/tasks/"))?.path;
    state.taskId = path.basename(taskPath ?? "", ".md");
    check("create_task crea Borrador", applied.applied === true && /^TASK-/.test(state.taskId));
    const planPath = path.join(state.iaRoot, "03_plan.md");
    await fs.appendFile(planPath, `\n### Fase 99 — Smoke ⏳ Planificada\n\n| Componente | Estado |\n|---|---|\n| Prueba | ⏳ ${state.taskId} |\n`, "utf8");
  },
};

export function taskSchema() {
  return {
    type: "object",
    properties: {
      title: { type: "string" }, area: { type: "string", enum: ["FE", "BE", "DB", "INF", "DOC", "MCP", "QA"] },
      priority: { type: "string", enum: ["critica", "alta", "media", "baja"] }, risk: { type: "string", enum: ["bajo", "medio", "alto"] },
      authorName: { type: "string" }, authorEmail: { type: "string" }, branch: { type: "string" }, context: { type: "string" }, objective: { type: "string" },
      allowedScope: { type: "array", items: { type: "string" } }, outOfScope: { type: "array", items: { type: "string" } }, acceptanceCriteria: { type: "array", items: { type: "string" } },
      likelyFiles: { type: "array", items: { type: "string" } }, technicalPlan: { type: "array", items: { type: "string" } }, steps: { type: "array", items: { type: "string" } },
      expectedOutput: { type: "string" }, validation: { type: "array", items: { type: "string" } }, rollback: { type: "string" }, dependencies: { type: "array", items: { type: "string" } }, notes: { type: "string" }, apply: { type: "boolean" },
    },
    required: ["title", "area", "context", "objective", "allowedScope", "outOfScope", "acceptanceCriteria", "technicalPlan", "steps", "expectedOutput", "validation", "rollback"],
    additionalProperties: false,
  };
}

function fixture() {
  return { title: "Validar cierre integral del workflow", area: "QA", priority: "baja", risk: "medio", context: "El smoke necesita una tarea desechable para verificar el ciclo completo.", objective: "Verificar creación, aprobación, trabajo y cierre en una copia temporal.", allowedScope: ["Modificar únicamente la copia temporal de /ia."], outOfScope: ["Modificar el repositorio real."], acceptanceCriteria: ["El cierre actualiza 03, 04 y 05."], technicalPlan: ["Crear y cerrar una tarea de prueba."], steps: ["Ejecutar el flujo público."], expectedOutput: "La tarea queda archivada y el contexto sincronizado.", validation: ["Smoke MCP completo."], rollback: "Eliminar la copia temporal.", likelyFiles: ["ia/03_plan.md"], dependencies: ["ninguna"] };
}
