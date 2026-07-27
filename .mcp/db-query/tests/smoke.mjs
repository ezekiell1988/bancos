#!/usr/bin/env node
import path from "node:path";
import { fileURLToPath } from "node:url";
import { runSmoke } from "../src/smoke.mjs";

const here = path.dirname(fileURLToPath(import.meta.url));
await runSmoke({
  serverPath: path.resolve(here, "../server.mjs"),
  projectRoot: path.resolve(here, "../../.."),
  serverName: "db-query-mcp",
  requiresApply: false,
});
