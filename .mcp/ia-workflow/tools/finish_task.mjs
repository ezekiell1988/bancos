import fs from "node:fs/promises";
import path from "node:path";
import { runtime } from "../src/runtime.mjs";

export default {
  name: "finish_task",
  order: 40,
  description: "Cierra una tarea o la mueve a revisión. Al cerrar sincroniza 03_plan, 04_tasks y 05_progress. Preview por defecto.",
  inputSchema: finishSchema(),
  handler: (args) => runtime.write.runWriteOperation("finish_task", args),
  async smoke({ callTool, check, toolJson, state }) {
    const args = { id: state.taskId, outcome: "done", summary: "Workflow integral validado correctamente", area: "QA", filesChanged: ["prueba.txt"], validation: ["Smoke ejecutado correctamente."], pendingItems: ["Ninguno."], risks: ["Ninguno adicional."], rollbackNotes: "Eliminar la copia temporal." };
    const preview = toolJson(await callTool("finish_task", args));
    const paths = preview.changes?.map((change) => change.path) ?? [];
    check("finish_task preview cubre 03", paths.includes("03_plan.md"));
    check("finish_task preview cubre 04", paths.some((item) => item.startsWith("04_tasks/")));
    check("finish_task preview cubre 05", paths.some((item) => item.startsWith("05_progress/")));
    const applied = toolJson(await callTool("finish_task", { ...args, apply: true }));
    check("finish_task aplica cierre", applied.applied === true);
    const plan = await fs.readFile(path.join(state.iaRoot, "03_plan.md"), "utf8");
    const current = await fs.readFile(path.join(state.iaRoot, "04_tasks/current.md"), "utf8");
    const progress = await fs.readFile(path.join(state.iaRoot, "05_progress/current.md"), "utf8");
    const quality = await fs.readFile(path.join(state.iaRoot, "05_progress/by-component/quality.md"), "utf8");
    check("finish_task actualiza 03 físicamente", !plan.includes(`⏳ ${state.taskId}`) && plan.includes("| Prueba | ✅ |"));
    check("finish_task limpia 04 current", !current.includes(`| ${state.taskId} |`));
    check("finish_task registra 05", progress.includes(state.taskId));
    check("finish_task registra componente de 05", quality.includes(state.taskId));
  },
};

export function finishSchema() {
  return { type: "object", properties: { id: { type: "string" }, outcome: { type: "string", enum: ["review", "done"] }, summary: { type: "string" }, area: { type: "string", enum: ["FE", "BE", "DB", "INF", "DOC", "MCP", "QA"] }, authorName: { type: "string" }, progressComponent: { type: "string", enum: ["backend", "frontend", "database", "infrastructure", "documentation", "quality"] }, filesChanged: { type: "array", items: { type: "string" } }, validation: { type: "array", items: { type: "string" } }, pendingItems: { type: "array", items: { type: "string" } }, risks: { type: "array", items: { type: "string" } }, rollbackNotes: { type: "string" }, apply: { type: "boolean" } }, required: ["id", "outcome", "summary", "area"], additionalProperties: false };
}
