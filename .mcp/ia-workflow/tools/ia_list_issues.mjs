import { runtime } from "../src/runtime.mjs";
export default {
  name: "ia_list_issues", description: "Lista issues abiertos y el índice 07_issues/current.md.",
  inputSchema: { type: "object", properties: { includeText: { type: "boolean" }, mode: { type: "string", enum: ["summary", "pathsOnly", "full"] }, maxChars: { type: "integer" } }, additionalProperties: false },
  handler: (args) => runtime.read.listIssues(args),
  async smoke({ callTool, check, toolJson }) { const result = toolJson(await callTool("ia_list_issues", { mode: "pathsOnly" })); check("ia_list_issues devuelve índice", result.current?.path === "07_issues/current.md"); check("ia_list_issues devuelve conteo", Number.isInteger(result.count)); },
};
