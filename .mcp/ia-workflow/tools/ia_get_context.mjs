import { runtime } from "../src/runtime.mjs";
export default {
  name: "ia_get_context", description: "Devuelve contexto compacto según intención: planificar, implementar, revisar, depurar o cerrar sesión.",
  inputSchema: { type: "object", properties: { intent: { type: "string", enum: ["planificar", "implementar", "revisar", "depurar", "cerrar_sesion"] }, taskId: { type: "string" }, issueId: { type: "string" }, includeText: { type: "boolean" }, mode: { type: "string", enum: ["full", "summary", "pathsOnly"] }, maxChars: { type: "integer" } }, required: ["intent"], additionalProperties: false },
  handler: (args) => runtime.read.getContext(args),
  async smoke({ callTool, check, toolJson }) { const result = toolJson(await callTool("ia_get_context", { intent: "planificar", mode: "pathsOnly", includeText: false })); check("ia_get_context devuelve rutas", result.files?.length > 0 && result.files.every((item) => item.path)); check("ia_get_context pathsOnly omite texto", result.files?.every((item) => !item.text && !item.summary)); },
};
