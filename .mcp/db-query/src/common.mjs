export class ToolError extends Error {}
export const log = (message) => process.stderr.write(`${message}\n`);
export function errorResult(message) { const payload = { error: String(message) }; return { content: [{ type: "text", text: JSON.stringify(payload, null, 2) }], structuredContent: payload, isError: true }; }
export function clamp(value, min, max, fallback) { const parsed = Number.parseInt(value, 10); return Number.isFinite(parsed) ? Math.min(max, Math.max(min, parsed)) : fallback; }
export function requiredString(value, name) { if (typeof value !== "string" || !value.trim()) throw new ToolError(`${name} requerido`); return value.trim(); }
