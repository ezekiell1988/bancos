import path from "node:path";
import { createDatabase } from "./database.mjs";
import { McpServer } from "./protocol.mjs";
import { createReportWriter } from "./report.mjs";
import { loadTools } from "./registry.mjs";

export async function startServer({ mcpRoot, profile, projectRoot, sql }) {
  const runtime = { profile, database: createDatabase({ profile, projectRoot, sql }), writeReport: createReportWriter({ mcpRoot, projectRoot }) };
  const tools = await loadTools({ toolsDir: path.join(mcpRoot, "tools"), runtime });
  new McpServer({ profile, tools }).start();
}

export function resolveProjectRoot(argv) {
  const index = argv.findIndex((arg) => arg === "--project-root");
  const direct = index >= 0 ? argv[index + 1] : argv.find((arg) => arg.startsWith("--project-root="))?.slice(15);
  return path.resolve(direct ?? process.env.DB_MCP_PROJECT_ROOT ?? process.cwd());
}