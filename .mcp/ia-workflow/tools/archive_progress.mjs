import { runtime } from "../src/runtime.mjs";
export default {
  name: "archive_progress", order: 55,
  description: "Mueve entradas antiguas de '## Completado en sesiones recientes' de 05_progress/current.md a archivos mensuales en 05_progress/archive/. keepDays configurable (default 7). Idempotente. Preview por defecto.",
  inputSchema: { type: "object", properties: { keepDays: { type: "integer", minimum: 0, default: 7 }, apply: { type: "boolean" } }, additionalProperties: false },
  handler: (args) => runtime.write.runWriteOperation("archive_progress", args),
  async smoke({ callTool, check, toolJson }) { const result = toolJson(await callTool("archive_progress", {})); check("archive_progress preview por defecto", result.applied === false); },
};
