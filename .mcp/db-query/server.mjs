#!/usr/bin/env node
import path from "node:path";
import { fileURLToPath } from "node:url";
import { resolveProjectRoot, startServer } from "./src/server.mjs";
import { profile, sql } from "./src/profile.mjs";

const here = path.dirname(fileURLToPath(import.meta.url));
await startServer({ mcpRoot: here, profile, projectRoot: resolveProjectRoot(process.argv.slice(2)), sql });
