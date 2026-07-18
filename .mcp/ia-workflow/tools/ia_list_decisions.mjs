import { runtime } from "../src/runtime.mjs";
export default {
  name: "ia_list_decisions", description: "Lista ADRs individuales con ID, título y ruta.",
  inputSchema: { type: "object", properties: { query: { type: "string" }, mode: { type: "string", enum: ["summary", "pathsOnly"] } }, additionalProperties: false },
  handler: (args) => runtime.read.listDecisions(args),
  async smoke({ callTool, check, toolJson }) { const result = toolJson(await callTool("ia_list_decisions", {})); check("ia_list_decisions devuelve ADRs", result.count > 0); check("ia_list_decisions incluye ADR-01", result.decisions?.some((item) => item.id === "ADR-01")); },
};
