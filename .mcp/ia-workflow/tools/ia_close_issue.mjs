import path from "node:path";
import { runtime } from "../src/runtime.mjs";
import { closeIssueSchema } from "./close_issue.mjs";
export default {
  name: "ia_close_issue", order: 60, description: "Primitiva interna para cerrar issue sincronizando 05 y 07. Preview por defecto.",
  inputSchema: closeIssueSchema(), handler: (args) => runtime.write.runWriteOperation("close_issue", args),
  async smoke({ callTool, check, toolJson }) { const created = toolJson(await callTool("ia_create_issue", { title: "Issue temporal interno", severity: "low", component: "MCP", symptom: "Se prueba el cierre interno.", apply: true })); const issuePath = created.changes?.find((change) => change.path.includes("07_issues/open/"))?.path; const id = path.basename(issuePath ?? "", ".md"); const preview = toolJson(await callTool("ia_close_issue", { id, resolution: "El issue se resolvió en la copia temporal.", rootCause: "Faltaba validar el cierre interno.", learning: "Las primitivas también requieren smoke." })); check("ia_close_issue cubre 05", preview.changes?.some((change) => change.path.startsWith("05_progress/"))); check("ia_close_issue cubre 07", preview.changes?.some((change) => change.path.startsWith("07_issues/"))); },
};
