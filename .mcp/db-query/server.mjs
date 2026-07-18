#!/usr/bin/env node
import path from "node:path";
import { fileURLToPath } from "node:url";
import { McpServer } from "./src/protocol.mjs";
import { loadTools } from "./src/registry.mjs";
const here = path.dirname(fileURLToPath(import.meta.url));
new McpServer({ tools: await loadTools(path.join(here, "tools")) }).start();
