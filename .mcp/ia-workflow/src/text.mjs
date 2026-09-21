// Helpers de texto/regex — puerto directo de los métodos estáticos privados de
// WorkflowService.cs (Timestamp, Require, RequireChoice, Field, SetField, etc.).
import { randomUUID } from 'node:crypto';
import { ToolError } from '../../_shared/results.mjs';

const SECRET_PATTERN = /(?:api[_-]?key|secret|token|password)\s*[:=]\s*['"][^'"]{8,}/i;

/** Equivalente a DateTimeOffset.Now con offset fijo -06:00 (Costa Rica, sin horario de verano). */
export function timestamp() {
  const now = new Date();
  const parts = new Intl.DateTimeFormat('en-US', {
    timeZone: 'America/Costa_Rica',
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
    hour12: false,
  }).formatToParts(now);
  const get = (type) => parts.find((p) => p.type === type).value;
  const ms = String(now.getMilliseconds()).padStart(3, '0');
  return `${get('year')}-${get('month')}-${get('day')}T${get('hour')}:${get('minute')}:${get('second')}.${ms}-06:00`;
}

export function require_(value, name) {
  if (value === null || value === undefined || value.trim() === '') {
    throw new ToolError(`${name} es obligatorio.`);
  }
  return value.trim();
}

export function requireChoice(value, choices, name) {
  const match = choices.find((choice) => choice.toLowerCase() === String(value).toLowerCase());
  if (!match) throw new ToolError(`${name} debe ser uno de: ${choices.join(', ')}.`);
  return match;
}

export function normalizeRisk(risk) {
  return requireChoice((risk ?? 'medio').toLowerCase(), ['bajo', 'medio', 'alto'], 'risk');
}

export function initials(name) {
  return name
    .split(' ')
    .filter((part) => part.length > 0)
    .slice(0, 3)
    .map((part) => part[0].toUpperCase())
    .join('');
}

export function normalizeInitials(value) {
  if (!/^[A-Za-z0-9]{1,3}$/.test(value)) {
    throw new ToolError('authorInitials debe tener entre 1 y 3 caracteres alfanuméricos.');
  }
  return value.toUpperCase();
}

export function slug(input) {
  const normalized = input
    .toLowerCase()
    .normalize('NFD')
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-|-$/g, '');
  return normalized === '' ? 'item' : normalized;
}

export function bullets(values, fallback) {
  const filtered = (values ?? []).filter((v) => v !== null && v !== undefined && v.trim() !== '');
  if (filtered.length > 0) return filtered.map((v) => `* ${v.trim()}`).join('\n');
  return `* ${fallback}`;
}

export function numbered(values) {
  return values.map((v, i) => `${i + 1}. ${v.trim()}`).join('\n');
}

export function taskId(text) {
  const m = /^#\s+(TASK-[A-Z0-9]{1,3}-[A-Z]+-\d+)/m.exec(text);
  return m ? m[1] : '';
}

export function field(text, name) {
  const m = new RegExp(`^\\*\\*${escapeRegExp(name)}:\\*\\*\\s*(.*)$`, 'm').exec(text);
  const raw = (m ? m[1] : '').trim();
  return raw.replace(/^[^\p{L}\p{N}]+/u, '').trim();
}

export function setField(text, name, value) {
  const pattern = new RegExp(`^\\*\\*${escapeRegExp(name)}:\\*\\*`, 'm');
  if (!pattern.test(text)) return text;
  return text.replace(new RegExp(`^\\*\\*${escapeRegExp(name)}:\\*\\*.*$`, 'm'), `**${name}:** ${value}`);
}

export function status(text) {
  return field(text, 'Estado');
}

export function ensureStatus(text, expected) {
  const current = status(text);
  if (current.toLowerCase() !== expected.toLowerCase()) {
    throw new ToolError(`La tarea debe estar en ${expected}; está en ${current}.`);
  }
}

export function truncate(text, maximum) {
  const clamped = Math.min(Math.max(maximum, 256), 50_000);
  const truncated = text.length > clamped;
  return { text: truncated ? text.slice(0, clamped) + '\n[truncado]' : text, truncated };
}

export function summary(text) {
  const headings = [...text.matchAll(/^#{1,6}\s+(.+)$/gm)].slice(0, 20).map((m) => m[1]);
  return { headings, chars: text.length, status: field(text, 'Estado') };
}

export function matchesTaskGroup(text, group) {
  const s = status(text);
  switch (group) {
    case 'active':
      return s === 'Lista' || s === 'En progreso';
    case 'backlog':
      return s.toLowerCase() === 'borrador';
    case 'blocked':
      return s.toLowerCase() === 'bloqueada';
    case 'done':
      return s === 'Completada' || s === 'Duplicada' || s === 'Eliminada';
    default:
      return true;
  }
}

function buildEventJson({ taskId: id, event, occurredAt, reason }) {
  return JSON.stringify({
    schemaVersion: 2,
    eventId: randomUUID(),
    taskId: id,
    event,
    occurredAt,
    actor: 'MCP iaWorkflow',
    reason,
  });
}

export function appendEvent(text, id, event, reason) {
  const json = buildEventJson({ taskId: id, event, occurredAt: timestamp(), reason });
  const blockPattern = /## Historial de eventos V2\s*\n\s*```jsonl\s*\n[\s\S]*?\n```/m;
  if (blockPattern.test(text)) {
    return text.replace(
      /(## Historial de eventos V2\s*\n\s*```jsonl\s*\n[\s\S]*?)(\n```)/m,
      (_, head, tail) => `${head}\n${json}${tail}`,
    );
  }
  return text.trimEnd() + `\n\n## Historial de eventos V2\n\n\`\`\`jsonl\n${json}\n\`\`\`\n`;
}

export function buildEventJsonExternal(id, event, occurredAt, reason) {
  return buildEventJson({ taskId: id, event, occurredAt, reason });
}

export function secretPatternMatches(content) {
  return SECRET_PATTERN.test(content);
}

export function escapeRegExp(value) {
  return value.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}
