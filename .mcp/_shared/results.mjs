// Resultados de tool, logging y error de negocio — compartidos por todos los servidores MCP.
// stdout es exclusivo del protocolo MCP: log() escribe siempre a stderr.

import { encodeToon } from './toon.mjs';

/** Error de negocio: se devuelve al modelo como { error: "..." } sin crashear el server. */
export class ToolError extends Error {}

let serverName = null;

/** Fija el nombre usado como prefijo de log(). Lo llama runServer() al arrancar. */
export function setServerName(name) {
  serverName = name;
}

/** Log a stderr — stdout es exclusivo del protocolo MCP. */
export function log(message) {
  process.stderr.write(`[${serverName ?? 'mcp'}] ${message}\n`);
}

/** Omite claves con valor null (undefined ya no se serializa) — igual que
 * JsonIgnoreCondition.WhenWritingNull, el default real de los hosts .NET que
 * este runtime reemplaza. Sin este replacer, los tools que devuelven `campo:
 * valor ?? null` producen JSON con más claves que la versión .NET equivalente. */
function omitNulls(_key, value) {
  return value === null ? undefined : value;
}

/** Resultado JSON estándar de un tool. */
export function textResult(payload) {
  const text = typeof payload === 'string' ? payload : JSON.stringify(payload, omitNulls, 1);
  return { content: [{ type: 'text', text }] };
}

/** Resultado TOON (arrays uniformes, ~40% menos tokens). */
export function toonResult(payload) {
  return { content: [{ type: 'text', text: encodeToon(payload) }] };
}

/** Error de negocio como contenido legible por el modelo. */
export function errorResult(message) {
  return { content: [{ type: 'text', text: JSON.stringify({ error: message }) }], isError: true };
}

export function clamp(value, minimum, maximum, fallback) {
  const numeric = Number(value ?? fallback);
  return Number.isFinite(numeric) ? Math.min(Math.max(Math.trunc(numeric), minimum), maximum) : fallback;
}
