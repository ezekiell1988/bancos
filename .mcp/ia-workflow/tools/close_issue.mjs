import fs from "node:fs/promises";
import path from "node:path";
import { runtime } from "../src/runtime.mjs";

export default {
  name: "close_issue",
  order: 70,
  description: "Cierra y archiva un issue abierto; sincroniza 05_progress y 07_issues. Preview por defecto.",
  inputSchema: closeIssueSchema(),
  handler: (args) => runtime.write.runWriteOperation("close_issue", args),
  async smoke({ callTool, check, toolJson, state }) {
    const created = toolJson(await callTool("ia_create_issue", { title: "Error temporal para cierre público", severity: "low", component: "MCP", symptom: "El smoke necesita comprobar el cierre de issues.", apply: true }));
    const issuePath = created.changes?.find((change) => change.path.includes("07_issues/open/"))?.path;
    const id = path.basename(issuePath ?? "", ".md");
    const args = { id, resolution: "El flujo fue corregido y validado.", rootCause: "Faltaba una operación de cierre sincronizada.", learning: "Todo cierre debe actualizar progreso e historial." };
    const preview = toolJson(await callTool("close_issue", args));
    const paths = preview.changes?.map((change) => change.path) ?? [];
    check("close_issue preview cubre 05", paths.includes("05_progress/current.md"));
    check("close_issue preview cubre 07", paths.some((item) => item.startsWith("07_issues/")));
    const applied = toolJson(await callTool("close_issue", { ...args, apply: true }));
    check("close_issue aplica cierre", applied.applied === true);
    const current = await fs.readFile(path.join(state.iaRoot, "07_issues/current.md"), "utf8");
    const progress = await fs.readFile(path.join(state.iaRoot, "05_progress/current.md"), "utf8");
    const componentProgress = await fs.readFile(path.join(state.iaRoot, "05_progress/by-component/documentation.md"), "utf8");
    check("close_issue limpia 07 current", !current.includes(`| ${id} |`));
    check("close_issue registra 05", progress.includes(id));
    check("close_issue registra componente de 05", componentProgress.includes(id));
  },
};

export function closeIssueSchema() {
  return { type: "object", properties: { id: { type: "string" }, resolution: { type: "string" }, rootCause: { type: "string" }, learning: { type: "string" }, progressComponent: { type: "string", enum: ["backend", "frontend", "database", "infrastructure", "documentation", "quality"] }, apply: { type: "boolean" } }, required: ["id", "resolution", "rootCause", "learning"], additionalProperties: false };
}
