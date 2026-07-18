#!/usr/bin/env node
import { readdirSync } from "node:fs";
import path from "node:path";
import { spawn } from "node:child_process";
import { fileURLToPath, pathToFileURL } from "node:url";
const here = path.dirname(fileURLToPath(import.meta.url));
const serverPath = path.resolve(here, "../server.mjs");
const toolsDir = path.resolve(here, "../tools");
const projectRoot = path.resolve(here, "../../..");
const child = spawn(process.execPath, [serverPath, "--project-root", projectRoot], { stdio: ["pipe", "pipe", "pipe"] });
let buffer = "", stderr = "", nextId = 1, failures = 0;
const pending = new Map();
child.stdout.on("data", (chunk) => { buffer += chunk.toString(); let end; while ((end = buffer.indexOf("\n")) !== -1) { const line = buffer.slice(0, end).trim(); buffer = buffer.slice(end + 1); if (!line) continue; const message = JSON.parse(line); pending.get(message.id)?.(message); pending.delete(message.id); } });
child.stderr.on("data", (chunk) => { stderr += chunk.toString(); });
function rpc(method, params = {}, timeoutMs = 20000) { const id = nextId++; return new Promise((resolve, reject) => { const timer = setTimeout(() => { pending.delete(id); reject(new Error(`timeout ${method}: ${stderr}`)); }, timeoutMs); pending.set(id, (message) => { clearTimeout(timer); resolve(message); }); child.stdin.write(`${JSON.stringify({ jsonrpc: "2.0", id, method, params })}\n`); }); }
const callTool = (name, args = {}) => rpc("tools/call", { name, arguments: args });
const toolJson = (response) => JSON.parse(response.result.content[0].text);
const toolText = (response) => response.result.content[0].text;
const toolStructured = (response) => response.result.structuredContent;
function check(name, condition, detail = "") { process.stdout.write(`${condition ? "OK  " : "FAIL"} ${name}${detail ? ` — ${detail}` : ""}\n`); if (!condition) failures += 1; }
try {
  const init = await rpc("initialize", { protocolVersion: "2025-06-18", capabilities: {}, clientInfo: { name: "smoke", version: "1" } });
  check("initialize responde", init.result?.serverInfo?.name === "db-query-mcp");
  const future = await rpc("initialize", { protocolVersion: "9999-99-99", capabilities: {}, clientInfo: { name: "smoke", version: "1" } });
  check("versión futura no se refleja", future.result?.protocolVersion !== "9999-99-99");
  const files = readdirSync(toolsDir).filter((file) => file.endsWith(".mjs") && !file.startsWith("_")).sort();
  const expected = files.map((file) => path.basename(file, ".mjs"));
  const listed = await rpc("tools/list");
  const names = listed.result.tools.map((tool) => tool.name);
  check("tools/list coincide con tools/", names.length === expected.length && expected.every((name) => names.includes(name)));
  check("db_status aparece primero", names[0] === "db_status");
  const modules = [];
  for (const file of files) modules.push((await import(pathToFileURL(path.join(toolsDir, file)).href)).default);
  modules.sort((a, b) => (a.order ?? 100) - (b.order ?? 100) || a.name.localeCompare(b.name));
  const state = {};
  const ctx = { rpc, callTool, check, toolJson, toolText, toolStructured, state };
  for (const tool of modules) { check(`${tool.name} tiene smoke co-ubicado`, typeof tool.smoke === "function"); if (typeof tool.smoke === "function") { try { await tool.smoke(ctx); } catch (error) { check(`smoke ${tool.name} no lanza`, false, error.message); } } }
  process.stdout.write(failures === 0 ? "\nSMOKE TEST: TODO OK\n" : `\nSMOKE TEST: ${failures} fallo(s)\n`);
  process.exitCode = failures === 0 ? 0 : 1;
} catch (error) { process.stderr.write(`SMOKE TEST ERROR: ${error.stack ?? error.message}\n${stderr}`); process.exitCode = 1; } finally { child.kill(); }
