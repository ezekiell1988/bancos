// Registry de autodescubrimiento de tools — copiar a .mcp/{servidor}/src/registry.mjs.
//
// Cada archivo tools/{name}.mjs (sin prefijo "_") exporta default un objeto con el
// contrato { name, description, inputSchema, handler, format?, order?, smoke? }.
// Un archivo inválido detiene el arranque con "tools/{archivo}: {motivo}" en stderr,
// que VS Code muestra en el Output del servidor MCP.

import { readdirSync } from 'node:fs';
import path from 'node:path';
import { pathToFileURL } from 'node:url';

const FORMATS = new Set(['json', 'toon']);

/** Lista los archivos de tool de una carpeta (mismo criterio que usa el smoke test). */
export function listToolFiles(toolsDir) {
  return readdirSync(toolsDir)
    .filter((f) => f.endsWith('.mjs') && !f.startsWith('_'))
    .sort();
}

/**
 * Carga y valida todos los tools de la carpeta.
 * @returns {Promise<Array>} tools ordenados por `order` (default 100) y luego nombre —
 *   ese es el orden que verá el cliente en tools/list.
 */
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
    fail('description requerida (el modelo la usa para decidir cuándo llamar el tool)');
  }
  if (tool.inputSchema?.type !== 'object') fail('inputSchema.type debe ser "object"');
  if (tool.inputSchema.additionalProperties !== false) {
    fail('inputSchema.additionalProperties debe ser false');
  }
  if (typeof tool.handler !== 'function') fail('handler debe ser una función');
  if (tool.format !== undefined && !FORMATS.has(tool.format)) {
    fail(`format "${tool.format}" inválido (usar 'toon' o 'json', u omitir)`);
  }
  if (tool.order !== undefined && typeof tool.order !== 'number') fail('order debe ser número');
  if (tool.smoke !== undefined && typeof tool.smoke !== 'function') fail('smoke debe ser una función');
}
