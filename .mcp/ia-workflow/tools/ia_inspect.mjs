import { runtime } from "../src/runtime.mjs";

const textMode = { type: "string", enum: ["full", "summary", "pathsOnly"] };
const maxChars = { type: "integer", minimum: 0, maximum: 50000 };

const variants = [
  {
    type: "object",
    properties: { action: { const: "list_tasks" }, status: { type: "string", enum: ["active", "backlog", "blocked", "done", "all"] }, mode: textMode },
    required: ["action"],
    additionalProperties: false,
  },
  {
    type: "object",
    properties: { action: { const: "read_task" }, id: { type: "string" }, mode: textMode, maxChars },
    required: ["action", "id"],
    additionalProperties: false,
  },
  {
    type: "object",
    properties: { action: { const: "list_decisions" }, query: { type: "string" }, mode: textMode },
    required: ["action"],
    additionalProperties: false,
  },
  {
    type: "object",
    properties: { action: { const: "read_decision" }, id: { type: "string" }, mode: textMode, maxChars },
    required: ["action", "id"],
    additionalProperties: false,
  },
  {
    type: "object",
    properties: { action: { const: "list_issues" }, mode: textMode, includeText: { type: "boolean" }, maxChars },
    required: ["action"],
    additionalProperties: false,
  },
  {
    type: "object",
    properties: { action: { const: "read_file" }, path: { type: "string" }, mode: textMode, maxChars },
    required: ["action", "path"],
    additionalProperties: false,
  },
  {
    type: "object",
    properties: { action: { const: "search" }, query: { type: "string" }, scope: { type: "string", enum: ["all", "tasks", "decisions", "issues", "progress", "context"] }, maxResults: { type: "integer", minimum: 1, maximum: 100 }, contextLines: { type: "integer", minimum: 0, maximum: 5 } },
    required: ["action", "query"],
    additionalProperties: false,
  },
  {
    type: "object",
    properties: { action: { const: "metrics" } },
    required: ["action"],
    additionalProperties: false,
  },
];

export default {
  name: "ia_inspect",
  order: 12,
  description: "Fachada de solo lectura para inspeccionar tareas, decisiones, issues, archivos, búsquedas y métricas del workflow.",
  inputSchema: { oneOf: variants },
  handler: async (args) => {
    switch (args.action) {
      case "list_tasks": return runtime.read.listTasks(args);
      case "read_task": return runtime.read.readTask(args);
      case "list_decisions": return runtime.read.listDecisions(args);
      case "read_decision": return runtime.read.readDecision(args);
      case "list_issues": return runtime.read.listIssues(args);
      case "read_file": return runtime.read.readFile(args);
      case "search": return runtime.read.searchIa(args);
      case "metrics": return buildMetrics();
      default: throw new Error(`Acción no soportada: ${args.action}`);
    }
  },
  async smoke({ callTool, check, toolJson }) {
    const result = toolJson(await callTool("ia_inspect", { action: "metrics" }));
    check("ia_inspect metrics devuelve estructura", typeof result.taskCounts === "object");
    check("ia_inspect metrics cuenta todos los estados", ["active", "backlog", "blocked", "done"].every((key) => Number.isInteger(result.taskCounts[key])));
  },
};

async function buildMetrics() {
  const all = await runtime.read.listTasks({ status: "all", mode: "summary" });
  const taskCounts = { active: 0, backlog: 0, blocked: 0, done: 0 };
  for (const [key, group] of Object.entries(all.groups)) {
    if (key === "active") taskCounts.active = group.files?.length ?? 0;
    if (key === "backlog") taskCounts.backlog = group.entries?.length ?? 0;
    if (key === "blocked") taskCounts.blocked = group.entries?.length ?? 0;
    if (key === "done") taskCounts.done = group.files?.length ?? 0;
  }
  return { taskCounts, iaValidation: await runtime.read.validateIa() };
}
