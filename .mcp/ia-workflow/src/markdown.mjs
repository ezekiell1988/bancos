// Manipulación de secciones Markdown V2 — puerto directo de los métodos privados de
// WorkflowService.cs que insertan/mueven filas de tabla, bullets y flags de revisión.
// Usa escaneo de líneas en vez de Regex(?ms).*?(?=^X|\z) para evitar la ambigüedad
// $ multilínea de JS frente a \z de .NET — el comportamiento resultante es idéntico.
import { ToolError } from '../../_shared/results.mjs';
import { require_, escapeRegExp, requireChoice } from './text.mjs';

/** Offsets de inicio de cada línea de `text` (para reconstruir slices por línea). */
function lineOffsets(text) {
  const lines = text.split('\n');
  const result = [];
  let offset = 0;
  for (const line of lines) {
    result.push({ offset, line });
    offset += line.length + 1;
  }
  return result;
}

/** Secciones delimitadas por líneas que matchean `headingTest`, hasta la próxima o fin de texto. */
function sectionsFor(text, headingTest) {
  const offsets = lineOffsets(text).filter((entry) => headingTest.test(entry.line));
  return offsets.map((entry, i) => {
    const start = entry.offset;
    const end = i + 1 < offsets.length ? offsets[i + 1].offset : text.length;
    return { start, end, text: text.slice(start, end), headingLine: entry.line };
  });
}

/** Sección `## {heading}` exacta hasta el próximo `## ` o fin de texto. Lanza si no existe. */
export function markdownSection(text, heading) {
  const sections = sectionsFor(text, /^## /);
  const pattern = new RegExp(`^## ${escapeRegExp(heading)}\\s*$`);
  const found = sections.find((s) => pattern.test(s.headingLine));
  if (!found) throw new ToolError(`No existe la sección ## ${heading}.`);
  return found;
}

/** Sección `### {phase}...` (heading puede tener sufijo de estado) hasta el próximo `### ` o fin. */
export function phaseSection(text, phase) {
  const sections = sectionsFor(text, /^###\s+/);
  const pattern = new RegExp(`^###\\s+${escapeRegExp(phase)}`);
  const found = sections.find((s) => pattern.test(s.headingLine));
  if (!found) throw new ToolError(`No existe la fase ${phase}.`);
  return found;
}

/** Igual que markdownSection pero retorna null en vez de lanzar (usado por AddProgress/AddBulletUnderHeading). */
function findSectionOrNull(text, heading) {
  const sections = sectionsFor(text, /^## /);
  const pattern = new RegExp(`^## ${escapeRegExp(heading)}\\s*$`);
  return sections.find((s) => pattern.test(s.headingLine)) ?? null;
}

export function insertPlanRow(section, component, statusLabel) {
  const lines = section.replace(/\s+$/, '').split('\n');
  const header = lines.findIndex((line) => line.includes('| Componente | Estado |'));
  if (header < 0) throw new ToolError('La fase no tiene la tabla canónica de componentes.');
  let insert = header + 2;
  while (insert < lines.length && lines[insert].trimStart().startsWith('|')) insert++;
  lines.splice(insert, 0, `| ${component.trim()} | ${statusLabel} |`);
  return lines.join('\n') + '\n';
}

export function insertTaskRow(current, section, id, area, risk) {
  const range = markdownSection(current, section);
  const row = `| [${id}](tasks/${id}.md) | ${area} | ${risk[0].toUpperCase()}${risk.slice(1)} |`;
  let text = range.text;
  if (text.includes(id)) return current;
  if (text.includes('Sin tareas registradas.')) {
    text = text.replace('Sin tareas registradas.', `| Tarea | Área | Riesgo |\n|-------|------|--------|\n${row}`);
  } else {
    const lines = text.replace(/\s+$/, '').split('\n');
    let lastRow = -1;
    for (let i = lines.length - 1; i >= 0; i--) {
      if (lines[i].trimStart().startsWith('|')) {
        lastRow = i;
        break;
      }
    }
    if (lastRow >= 0) lines.splice(lastRow + 1, 0, row);
    else lines.push('| Tarea | Área | Riesgo |', '|-------|------|--------|', row);
    text = lines.join('\n') + '\n';
  }
  return current.slice(0, range.start) + text + current.slice(range.end);
}

export function removeTaskRow(current, id) {
  let cleaned = current
    .split('\n')
    .filter((line) => !line.toLowerCase().includes(id.toLowerCase()))
    .join('\n');
  for (const section of ['Borradores', 'En progreso', 'Lista', 'Bloqueadas']) {
    const range = markdownSection(cleaned, section);
    let part = range.text;
    const rowCount = (part.match(/^\|/gm) ?? []).length;
    if (!part.includes('|') || rowCount <= 2) {
      part = `## ${section}\n\nSin tareas registradas.\n`;
    }
    cleaned = cleaned.slice(0, range.start) + part + cleaned.slice(range.end);
  }
  return cleaned.replace(/\s+$/, '') + '\n';
}

export function moveTaskRow(current, id, target, area, risk) {
  return insertTaskRow(removeTaskRow(current, id), target, id, area, risk);
}

export function addIndexLine(text, id, detail) {
  const base = !text || text.trim() === '' ? '> Última actualización: —\n\nSin registros.\n' : text;
  if (base.toLowerCase().includes(id.toLowerCase())) return base;
  return base.replace(/\s+$/, '') + `\n\n* ${id} — ${detail.trim()}\n`;
}

export function removeIndexLine(text, id) {
  return (
    text
      .split('\n')
      .filter((line) => !line.toLowerCase().includes(id.toLowerCase()))
      .join('\n')
      .replace(/\s+$/, '') + '\n'
  );
}

export function addProgress(text, entry) {
  const base =
    !text || text.trim() === ''
      ? '# Progreso\n\n## Completado en sesiones recientes\n\nSin entradas registradas.\n'
      : text;
  const section = findSectionOrNull(base, 'Completado en sesiones recientes');
  if (!section) return base.replace(/\s+$/, '') + '\n\n## Completado en sesiones recientes\n\n* ' + entry.trim() + '\n';
  const updated = section.text.replace('Sin entradas registradas.', '').replace(/\s+$/, '') + '\n* ' + entry.trim() + '\n';
  return base.slice(0, section.start) + updated + base.slice(section.end);
}

export function addBulletUnderHeading(text, heading, id, reason) {
  const row = `* ${id} — ${require_(reason, 'reason')}`;
  const section = findSectionOrNull(text, heading);
  if (!section) return text.replace(/\s+$/, '') + `\n\n## ${heading}\n\n${row}\n`;
  if (section.text.toLowerCase().includes(id.toLowerCase())) return text;
  const updated = section.text.replace('* Ninguno.', '').replace(/\s+$/, '') + '\n' + row + '\n';
  return text.slice(0, section.start) + updated + text.slice(section.end);
}

export function addReviewFlag(text, phase, review) {
  const heading =
    review === 'architecture' ? '## Fases pendientes de revisión arquitectónica' : '## Fases pendientes de retrospectiva';
  const line = `* ${phase} completada — pendiente de ${review}.`;
  if (text.includes(line)) return text;
  if (text.includes(heading)) return text.split(heading).join(heading + '\n\n' + line);
  return text.replace(/\s+$/, '') + '\n\n' + heading + '\n\n' + line + '\n';
}

export function extractArchivedTask(text, id) {
  const offsets = lineOffsets(text);
  const headingTest = new RegExp(`^#\\s+${escapeRegExp(id)}\\s+—`);
  const startEntry = offsets.find((entry) => headingTest.test(entry.line));
  if (!startEntry) throw new ToolError(`No se pudo extraer ${id} del historial.`);
  const startIndex = offsets.indexOf(startEntry);
  let end = text.length;
  for (let i = startIndex + 1; i < offsets.length; i++) {
    if (offsets[i].line.trim() === '---') {
      end = offsets[i].offset;
      break;
    }
  }
  return text.slice(startEntry.offset, end).replace(/\s+$/, '') + '\n';
}

export function markPlanTaskComplete(plan, id) {
  const sections = sectionsFor(plan, /^###\s+/);
  const found = sections.find((s) => s.text.includes(id));
  if (!found) return { plan, phaseCompleted: false, phaseName: null };

  let changed = found.text.replace(new RegExp(`⏳\\s+${escapeRegExp(id)}`, 'g'), `✅ ${id}`);
  const incomplete = /(?:⏳|🔄)\s+TASK-/.test(changed);
  const phaseCompleted = !incomplete;
  const titleMatch = /^###\s+(.+)$/.exec(found.headingLine);
  const rawTitle = titleMatch ? titleMatch[1] : '';
  const phaseName = rawTitle.replace(/\s+[✅⏳🔄].*$/, '').trim();
  if (phaseCompleted) {
    changed = changed.replace(
      /(###\s+.+?)\s+(?:⏳\s+Pendiente|🔄\s+En curso|Planificada)/,
      '$1 ✅ Completada',
    );
  }
  return { plan: plan.slice(0, found.start) + changed + plan.slice(found.end), phaseCompleted, phaseName };
}

export function independentPlanRelativePath(planFile) {
  if (!/^plan-[a-z0-9-]+\.md$/.test(planFile ?? '')) {
    throw new ToolError('planFile debe ser un archivo plan-*.md directamente dentro de ia/03_plan.');
  }
  return `03_plan/${planFile}`;
}

export function updateIndependentPlanStatus(plan, statusValue) {
  const normalized = requireChoice(statusValue, ['open', 'in_progress', 'completed'], 'planStatus');
  const label = normalized === 'open' ? '📝 Plan abierto' : normalized === 'in_progress' ? '🔄 En progreso' : '✅ Plan completado';
  const matches = [...plan.matchAll(/^>\s*\*\*Estado:\*\*.*$/gm)];
  if (matches.length !== 1) throw new ToolError("El plan debe tener exactamente una línea '> **Estado:**'.");
  const m = matches[0];
  return plan.slice(0, m.index) + `> **Estado:** ${label}` + plan.slice(m.index + m[0].length);
}

export function updateIndependentPlanTask(plan, update) {
  const taskId = update.taskId ?? '';
  if (!/^TASK-[A-Z0-9]{1,3}-[A-Z]+-\d+$/i.test(taskId)) throw new ToolError('taskUpdates.taskId no es un TASK-ID válido.');
  const state = requireChoice(update.status, ['draft', 'ready', 'in_progress', 'completed', 'blocked'], 'taskUpdates.status');
  const pattern = new RegExp(`^\\|[^\\r\\n]*\\b${escapeRegExp(taskId)}\\b[^\\r\\n]*\\|\\s*$`, 'gm');
  const matches = [...plan.matchAll(pattern)];
  if (matches.length !== 1) throw new ToolError(`${taskId} debe aparecer exactamente una vez en una fila de tabla del plan.`);
  const m = matches[0];
  const cells = m[0].split('|');
  if (cells.length < 3) throw new ToolError(`La fila de ${taskId} no tiene el formato de tabla esperado.`);
  const normalizedId = taskId.toUpperCase();
  const labels = {
    draft: ` 📝 ${normalizedId} — Borrador `,
    ready: ` 📝 ${normalizedId} — Lista `,
    in_progress: ` 🔄 ${normalizedId} — en progreso `,
    completed: ` ✅ ${normalizedId} — completada `,
    blocked: ` ⛔ ${normalizedId} — Bloqueada `,
  };
  cells[cells.length - 2] = labels[state];
  const replacement = cells.join('|');
  return plan.slice(0, m.index) + replacement + plan.slice(m.index + m[0].length);
}

export function updateIndependentPlanPhase(plan, update) {
  const title = update.phaseTitle;
  if (!title || title.trim() === '' || title.length > 160 || title.includes('\n') || title.includes('\r')) {
    throw new ToolError('phaseUpdates.phaseTitle debe ser un encabezado de fase de una sola línea.');
  }
  const suffix = {
    planned: '📝 Planificada',
    in_progress: '🔄 En progreso',
    completed: '✅ Completada',
    blocked: '⛔ Bloqueada',
  }[requireChoice(update.status, ['planned', 'in_progress', 'completed', 'blocked'], 'phaseUpdates.status')];
  const escapedTitle = escapeRegExp(title.trim());
  const pattern = new RegExp(
    `^###\\s+${escapedTitle}(?:\\s+(?:📝 Planificada|🔄 En progreso|✅ Completada|⛔ Bloqueada))?\\s*$`,
    'gm',
  );
  const matches = [...plan.matchAll(pattern)];
  if (matches.length !== 1) throw new ToolError(`La fase '${title}' debe aparecer exactamente una vez como encabezado de nivel 3.`);
  const m = matches[0];
  return plan.slice(0, m.index) + `### ${title.trim()} ${suffix}` + plan.slice(m.index + m[0].length);
}
