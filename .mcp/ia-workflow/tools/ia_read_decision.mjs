import { runtime } from "../src/runtime.mjs";
export default {
  name: "ia_read_decision", description: "Lee un ADR por ID en modo full o summary.",
  inputSchema: { type: "object", properties: { id: { type: "string" }, mode: { type: "string", enum: ["full", "summary"] }, maxChars: { type: "integer" } }, required: ["id"], additionalProperties: false },
  handler: (args) => runtime.read.readDecision(args),
  async smoke({ callTool, check, toolJson }) { const result = toolJson(await callTool("ia_read_decision", { id: "ADR-01", mode: "summary" })); check("ia_read_decision devuelve ADR-01", result.id === "ADR-01"); check("ia_read_decision devuelve contenido", Boolean(result.summary)); },
};
