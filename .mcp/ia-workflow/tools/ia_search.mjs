import { runtime } from "../src/runtime.mjs";
export default {
  name: "ia_search", description: "Busca texto exacto normalizado en Markdown local sin RAG y con límite de resultados.",
  inputSchema: { type: "object", properties: { query: { type: "string" }, scope: { type: "string", enum: ["all", "tasks", "decisions", "issues", "progress", "context"] }, maxResults: { type: "integer" }, contextLines: { type: "integer" } }, required: ["query"], additionalProperties: false },
  handler: (args) => runtime.read.searchIa(args),
  async smoke({ callTool, check, toolJson }) { const result = toolJson(await callTool("ia_search", { query: "Bancos", scope: "context", maxResults: 5 })); check("ia_search encuentra contexto", result.count > 0); check("ia_search respeta límite", result.results?.length <= 5); },
};
