import { runtime } from "../src/runtime.mjs";
export default {
  name: "ia_read_file", description: "Lee un Markdown por ruta relativa confinada a /ia.",
  inputSchema: { type: "object", properties: { path: { type: "string" }, mode: { type: "string", enum: ["full", "summary"] }, maxChars: { type: "integer" } }, required: ["path"], additionalProperties: false },
  handler: (args) => runtime.read.readFile(args),
  async smoke({ callTool, check, toolJson }) { const result = toolJson(await callTool("ia_read_file", { path: "00_context.md", mode: "summary" })); check("ia_read_file lee contexto", result.path === "00_context.md" && Boolean(result.summary)); const traversal = toolJson(await callTool("ia_read_file", { path: "../README.md" })); check("ia_read_file rechaza traversal", typeof traversal.error === "string" && traversal.error.includes("fuera")); },
};
