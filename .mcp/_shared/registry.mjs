// Registry de autodescubrimiento de tools.
// Cada tools/{name}.mjs (sin prefijo "_") exporta default
// { name, description, inputSchema, handler, format?, order?, smoke? }.

import { readdirSync } from 'node:fs';
import path from 'node:path';
import { pathToFileURL } from 'node:url';

const FORMATS = new Set(['json', 'toon']);

/** Lista los archivos de tool (mismo criterio que usa el smoke test). */
export function listToolFiles(toolsDir) {
  return readdirSync(toolsDir)
    .filter((f) => f.endsWith('.mjs') && !f.startsWith('_'))
    .sort();
}

/** Carga y valida todos los tools, ordenados por `order` (default 100) y nombre. */
export async function loadTools(toolsDir) {
  const tools = [];
  for (const file of listToolFiles(toolsDir)) {
    const mod = await import(pathToFileURL(path.join(toolsDir, file)).href);
    const tool = mod.default;
    validateTool(tool, path.basename(file, '.mjs'), file);
    tools.push(tool);
  }
  return tools.sort(
    (a, b) => (a.order ?? 100) - (b.order ?? 100) || a.name.localeCompare(b.name),
  );
}

function validateTool(tool, expectedName, file) {
  const fail = (msg) => {
    throw new Error(`tools/${file}: ${msg}`);
  };
  if (!tool || typeof tool !== 'object') fail('debe tener "export default { ... }"');
  if (tool.name !== expectedName) {
    fail(`name "${tool.name}" debe coincidir con el nombre del archivo ("${expectedName}")`);
  }
  if (!/^[a-z][a-z0-9_]*$/.test(tool.name)) fail('name debe ser snake_case');
  if (typeof tool.description !== 'string' || !tool.description.trim()) {
    fail('description requerida');
  }
  const schema = tool.inputSchema;
  if (schema?.type !== 'object') fail('inputSchema.type debe ser "object"');
  if (!Array.isArray(schema.oneOf) && schema.additionalProperties !== false) {
    fail('inputSchema.additionalProperties debe ser false');
  }
  if (Array.isArray(schema.oneOf)) {
    for (const v of schema.oneOf) {
      if (v.additionalProperties !== false) fail('cada variante oneOf debe cerrar additionalProperties');
      const hasConst = Object.values(v.properties ?? {}).some((p) => p.const !== undefined);
      if (!hasConst) fail('cada variante oneOf necesita una propiedad discriminante con const');
    }
  }
  assertArrayItems(schema, `tools/${file}`);
  if (typeof tool.handler !== 'function') fail('handler debe ser una función');
  if (tool.order !== undefined && typeof tool.order !== 'number') fail('order debe ser número');
  if (tool.smoke !== undefined && typeof tool.smoke !== 'function') fail('smoke debe ser una función');
  if (tool.format !== undefined && !FORMATS.has(tool.format)) {
    fail(`format "${tool.format}" inválido`);
  }
}

/** Todo type:"array" debe declarar items — recursivo por properties/items/oneOf. */
function assertArrayItems(schema, ctx) {
  if (!schema || typeof schema !== 'object') return;
  if (schema.type === 'array' && !schema.items) {
    throw new Error(`${ctx}: parámetro array sin "items"`);
  }
  for (const sub of Object.values(schema.properties ?? {})) assertArrayItems(sub, ctx);
  if (schema.items) assertArrayItems(schema.items, ctx);
  for (const v of schema.oneOf ?? []) assertArrayItems(v, ctx);
}
