export class ToolError extends Error {}

export function clamp(value, minimum, maximum, fallback) {
  const numeric = Number(value ?? fallback);
  return Number.isFinite(numeric) ? Math.min(Math.max(Math.trunc(numeric), minimum), maximum) : fallback;
}

export function requiredString(value, name) {
  const result = String(value ?? "").trim();
  if (!result) throw new ToolError(`${name} requerido`);
  return result;
}

export function errorResult(message) {
  return {
    content: [{ type: "text", text: JSON.stringify({ error: message }, null, 2) }],
    structuredContent: { error: message },
    isError: true,
  };
}

export function log(message) {
  process.stderr.write(`[db-query] ${message}\n`);
}