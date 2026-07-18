import { runtime } from "../src/runtime.mjs";

export default {
  name: "migrate_task",
  order: 15,
  description: "Migra una tarea Borrador de formato legado al contrato canónico. Preview por defecto; aplica solo con apply=true.",
  inputSchema: {
    type: "object",
    properties: {
      id: { type: "string" }, expectedOutput: { type: "string" }, area: { type: "string", enum: ["FE", "BE", "DB", "INF", "DOC", "MCP", "QA"] },
      priority: { type: "string", enum: ["critica", "alta", "media", "baja"] }, risk: { type: "string", enum: ["bajo", "medio", "alto"] },
      authorName: { type: "string" }, branch: { type: "string" }, likelyFiles: { type: "array", items: { type: "string" } }, dependencies: { type: "array", items: { type: "string" } },
      allowedScope: { type: "array", items: { type: "string" } }, outOfScope: { type: "array", items: { type: "string" } }, acceptanceCriteria: { type: "array", items: { type: "string" } }, technicalPlan: { type: "array", items: { type: "string" } }, validation: { type: "array", items: { type: "string" } }, rollback: { type: "string" }, apply: { type: "boolean" },
    },
    required: ["id", "expectedOutput"],
    additionalProperties: false,
  },
  handler: (args) => runtime.write.runWriteOperation("migrate_task", args),
  async smoke({ callTool, check, toolJson }) {
    const result = toolJson(await callTool("migrate_task", {
      id: "TASK-EZ-BE-01",
      area: "BE",
      expectedOutput: "La tarea cumple el contrato canónico y puede aprobarse.",
    }));
    check("migrate_task genera preview", result.applied === false && result.operation === "migrate_task");
    const applied = toolJson(await callTool("migrate_task", {
      id: "TASK-EZ-BE-01",
      area: "BE",
      expectedOutput: "La tarea cumple el contrato canónico y puede aprobarse.",
      apply: true,
    }));
    const migrated = toolJson(await callTool("ia_read_task", { id: "TASK-EZ-BE-01", mode: "full" }));
    check("migrate_task normaliza contrato", applied.applied === true && migrated.text?.includes("## Alcance permitido") && migrated.text?.includes("## Salida esperada"));
  },
};
