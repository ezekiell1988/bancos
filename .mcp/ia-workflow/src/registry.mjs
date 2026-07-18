import { readdirSync } from "node:fs";
import path from "node:path";
import { pathToFileURL } from "node:url";

const FORMATS = new Set(["json", "toon"]);

export function listToolFiles(toolsDir) {
  return readdirSync(toolsDir)
    .filter((file) => file.endsWith(".mjs") && !file.startsWith("_"))
    .sort();
}

export async function loadTools(toolsDir) {
  const tools = [];
  for (const file of listToolFiles(toolsDir)) {
    const module = await import(pathToFileURL(path.join(toolsDir, file)).href);
    const tool = module.default;
    validateTool(tool, path.basename(file, ".mjs"), file);
    tools.push(tool);
  }

  return tools.sort((a, b) => (a.order ?? 100) - (b.order ?? 100) || a.name.localeCompare(b.name));
}

function validateTool(tool, expectedName, file) {
  const fail = (message) => {
    throw new Error(`tools/${file}: ${message}`);
  };

  if (!tool || typeof tool !== "object") fail("debe exportar un objeto default");
  if (tool.name !== expectedName) fail(`name debe ser ${expectedName}`);
  if (!/^[a-z][a-z0-9_]*$/.test(tool.name)) fail("name debe ser snake_case");
  if (typeof tool.description !== "string" || !tool.description.trim()) fail("description requerida");
  if (tool.inputSchema?.type !== "object") fail("inputSchema.type debe ser object");
  if (tool.inputSchema.additionalProperties !== false) fail("additionalProperties debe ser false");
  if (typeof tool.handler !== "function") fail("handler debe ser función");
  if (tool.format !== undefined && !FORMATS.has(tool.format)) fail("format inválido");
  if (tool.order !== undefined && typeof tool.order !== "number") fail("order debe ser número");
  if (tool.smoke !== undefined && typeof tool.smoke !== "function") fail("smoke debe ser función");
}
