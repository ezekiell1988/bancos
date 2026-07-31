import { runtime } from "../src/runtime.mjs";
export default {
  name: "archive_progress", order: 55,
  description: "Mueve entradas antiguas de '## Completado en sesiones recientes' de 05_progress/current.md a archivos mensuales en 05_progress/archive/. keepDays configurable (default 7). Idempotente. Preview por defecto.",
  inputSchema: { type: "object", properties: { keepDays: { type: "integer", minimum: 0, default: 7 }, apply: { type: "boolean" } }, additionalProperties: false },
  handler: (args) => runtime.write.runWriteOperation("archive_progress", args),
  async smoke({ callTool, check, toolJson, state }) {
    const result = toolJson(await callTool("archive_progress", {}));
    check("archive_progress preview por defecto", result.applied === false);
    const currentPath = `${state.iaRoot}/05_progress/current.md`;
    const current = await state.fs.readFile(currentPath, "utf8");
    if (current.length > 12000) {
      const applied = toolJson(await callTool("archive_progress", { apply: true }));
      const reduced = await state.fs.readFile(currentPath, "utf8");
      const secondPreview = toolJson(await callTool("archive_progress", {}));
      check("archive_progress reduce current sobre limite", applied.applied === true && reduced.length <= 12000);
      check("archive_progress es idempotente", secondPreview.changes?.length === 0);
    }
  },
};
