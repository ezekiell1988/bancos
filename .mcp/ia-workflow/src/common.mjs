export function rpcError(code, message) {
  const error = new Error(message);
  error.jsonRpcError = { code, message };
  return error;
}

export class ToolError extends Error {}

export function textResult(payload) {
  return {
    content: [{ type: "text", text: JSON.stringify(payload, null, 2) }],
    structuredContent: payload,
    isError: false,
  };
}

export function errorResult(message) {
  const payload = { error: String(message) };
  return {
    content: [{ type: "text", text: JSON.stringify(payload, null, 2) }],
    structuredContent: payload,
    isError: true,
  };
}

export function log(message) {
  process.stderr.write(`${message}\n`);
}

export function requireString(value, name) {
  if (typeof value !== "string" || value.trim() === "") {
    throw rpcError(-32602, `${name} debe ser string no vacio`);
  }

  return value.trim();
}

export function requireEnum(value, name, values) {
  const text = requireString(value, name);
  if (!values.includes(text)) {
    throw rpcError(-32602, `${name} debe ser uno de: ${values.join(", ")}`);
  }

  return text;
}

export function requireSpanishText(value, name) {
  const text = requireString(value, name);
  if (looksMostlyEnglish(text)) {
    throw rpcError(-32602, `${name} parece estar en ingles; /ia debe mantenerse en espanol.`);
  }

  return text;
}

export function requireStringArray(value, name) {
  if (!Array.isArray(value) || value.length === 0) {
    throw rpcError(-32602, `${name} debe ser un arreglo no vacio`);
  }

  return value.map((item, index) => requireSpanishText(item, `${name}[${index}]`));
}

export function clampInteger(value, min, max) {
  const parsed = Number.parseInt(value, 10);
  if (Number.isNaN(parsed)) {
    return min;
  }

  return Math.min(max, Math.max(min, parsed));
}

export function compactMode(mode) {
  const value = String(mode ?? "full");
  if (!["full", "summary", "pathsOnly"].includes(value)) {
    throw rpcError(-32602, `Modo no soportado: ${mode}`);
  }

  return value;
}

export function sanitizeTaskId(id) {
  const value = String(id).trim();
  if (!/^TASK-[A-Z]{1,3}-[A-Z]{2,4}-\d{2,}$/.test(value)) {
    throw rpcError(-32602, `TASK id invalido: ${id}`);
  }

  return value;
}

export function sanitizeAdrId(id) {
  const value = String(id).trim().toUpperCase();
  if (!/^ADR-\d+$/.test(value)) {
    throw rpcError(-32602, `ADR id invalido: ${id}`);
  }

  return value;
}

export function sanitizeIssueId(id) {
  const value = String(id).trim();
  if (!/^[A-Za-z0-9._-]+$/.test(value)) {
    throw rpcError(-32602, `Issue id invalido: ${id}`);
  }

  return value;
}

export function normalizePriority(priority) {
  const value = String(priority).trim().toLowerCase();
  const map = {
    critica: "crítica",
    crítica: "crítica",
    alta: "alta",
    media: "media",
    baja: "baja",
  };

  if (!map[value]) {
    throw rpcError(-32602, `Prioridad no soportada: ${priority}`);
  }

  return map[value];
}

export function initialsFromName(name) {
  const initials = String(name)
    .replace(/[^\p{L}\s]/gu, "")
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 3)
    .map((part) => part[0].toLocaleUpperCase("es-CR"))
    .join("");

  return initials || "LLM";
}

export function normalizeSearch(value) {
  return String(value).toLocaleLowerCase("es-CR").normalize("NFKD").replace(/\p{Diacritic}/gu, "");
}

export function localePathSort(a, b) {
  return a.localeCompare(b, "es-CR");
}

export function looksMostlyEnglish(text) {
  const sample = normalizeSearch(text.slice(0, 2000));
  const englishHits = [" the ", " and ", " should ", " must ", " with ", " without ", " task ", " issue "].filter((word) =>
    sample.includes(word)
  ).length;
  const spanishHits = [" el ", " la ", " de ", " que ", " para ", " con ", " sin ", " tarea ", " debe "].filter((word) =>
    sample.includes(word)
  ).length;

  return englishHits >= 3 && englishHits > spanishHits + 1;
}

export function slugify(text) {
  return normalizeSearch(text)
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "")
    .slice(0, 90) || "decision";
}

export function capitalize(text) {
  const value = String(text);
  return value.charAt(0).toLocaleUpperCase("es-CR") + value.slice(1);
}

export function escapeRegExp(text) {
  return String(text).replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
}
