import { readdirSync } from "node:fs";
import path from "node:path";
import { pathToFileURL } from "node:url";

export async function loadTools({ toolsDir, runtime }) {
  const files = readdirSync(toolsDir)
    .filter((file) => file.endsWith(".mjs") && !file.startsWith("_"))
    .sort();
  const tools = [];

  for (const file of files) {
    const module = await import(pathToFileURL(path.join(toolsDir, file)).href);
    validate(module, path.basename(file, ".mjs"), file);
    tools.push({ ...module.definition, handler: (args) => module.execute(runtime, args) });
  }

  return tools.sort((left, right) => (left.order ?? 100) - (right.order ?? 100) || left.name.localeCompare(right.name));
}

function validate(module, expectedName, file) {
  const fail = (message) => { throw new Error(`tools/${file}: ${message}`); };
  const exports = Object.keys(module).sort();
  if (exports.length !== 2 || exports[0] !== "definition" || exports[1] !== "execute") fail("debe exportar únicamente definition y execute");
  const { definition } = module;
  if (!definition || typeof definition !== "object") fail("definition requerida");
  if (definition.name !== expectedName) fail(`definition.name debe ser ${expectedName}`);
  if (!/^[a-z][a-z0-9_]*$/.test(definition.name)) fail("definition.name debe ser snake_case");
  if (!definition.description?.trim()) fail("definition.description requerida");
  if (definition.inputSchema?.type !== "object" || definition.inputSchema.additionalProperties !== false) fail("definition.inputSchema debe ser object con additionalProperties:false");
  if (typeof module.execute !== "function") fail("execute requerido");
}