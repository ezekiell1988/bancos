import { compactMode, clampInteger } from "./common.mjs";

export function firstMarkdownTitle(text) {
  return text
    .split(/\r?\n/)
    .map((line) => line.trim())
    .find((line) => line.startsWith("# "))
    ?.replace(/^#\s+/, "");
}

export function resourceTitle(relativePath) {
  if (relativePath === "README.md") {
    return "Router /ia";
  }

  if (/^\d\d_/.test(relativePath.split("/").pop() ?? "")) {
    return (relativePath.split("/").pop() ?? "").replace(/\.md$/, "").replaceAll("_", " ");
  }

  return (relativePath.split("/").pop() ?? relativePath).replace(/\.md$/, "");
}

export function resourceDescription(relativePath) {
  if (relativePath.startsWith("04_tasks/tasks/")) {
    return "Detalle de tarea activa";
  }

  if (relativePath.startsWith("06_decisions/")) {
    return "Decision arquitectonica individual";
  }

  if (relativePath.startsWith("07_issues/open/")) {
    return "Issue abierto";
  }

  return "Archivo Markdown dentro de /ia";
}

export function guidanceForIntent(intent) {
  const guidance = {
    planificar: "Usa este contexto para elegir o proponer la siguiente tarea sin leer todo el repositorio.",
    implementar: "Lee la TASK especifica y ADRs relacionados antes de editar codigo.",
    revisar: "Prioriza bugs, regresiones, decisiones incumplidas y pruebas faltantes.",
    depurar: "Acota el issue con arquitectura e issues activos antes de modificar codigo.",
    cerrar_sesion: "Actualiza solo los archivos /ia necesarios y preserva historicos/ADRs.",
  };

  return guidance[intent];
}

export function formatTextPayload(text, options) {
  const mode = compactMode(options.mode ?? "full");
  const maxChars = clampInteger(options.maxChars ?? 12000, 0, 50000);

  if (mode === "pathsOnly") {
    return {};
  }

  if (mode === "summary") {
    return {
      summary: summarizeMarkdown(text),
      chars: text.length,
      truncated: false,
    };
  }

  const clipped = maxChars > 0 && text.length > maxChars;
  return {
    text: clipped ? `${text.slice(0, maxChars)}\n\n[truncado: ${text.length - maxChars} caracteres restantes]` : text,
    chars: text.length,
    truncated: clipped,
  };
}

export function summarizeMarkdown(text) {
  const lines = text.split(/\r?\n/);
  const headings = lines
    .filter((line) => /^#{1,3}\s+/.test(line.trim()))
    .slice(0, 8)
    .map((line) => line.trim());
  const bullets = lines
    .filter((line) => /^\s*[*-]\s+/.test(line))
    .slice(0, 8)
    .map((line) => line.trim());
  const tables = lines.filter((line) => line.trim().startsWith("|")).slice(0, 6).map((line) => line.trim());

  return {
    title: firstMarkdownTitle(text) ?? null,
    headings,
    bullets,
    tables,
  };
}

export function lineContext(lines, index, contextLines) {
  const start = Math.max(0, index - contextLines);
  const end = Math.min(lines.length, index + contextLines + 1);
  return lines.slice(start, end).map((text, offset) => ({
    line: start + offset + 1,
    text: text.trim(),
  }));
}

export function updateLastUpdatedLine(text, value) {
  if (!text) {
    return text;
  }

  if (/^> (?:\*\*)?Última actualización:(?:\*\*)? .+$/m.test(text)) {
    return text.replace(/^> (?:\*\*)?Última actualización:(?:\*\*)? .+$/m, `> **Última actualización:** ${value}`);
  }

  const lines = text.split(/\r?\n/);
  const titleIndex = lines.findIndex((line) => line.startsWith("# "));
  if (titleIndex !== -1) {
    lines.splice(titleIndex + 1, 0, "", `> **Última actualización:** ${value}`);
    return lines.join("\n");
  }

  return `> **Última actualización:** ${value}\n\n${text}`;
}

export function insertTableRow(text, heading, row) {
  const lines = text.split(/\r?\n/);
  const headingIndex = lines.findIndex((line) => line.trim() === heading);
  const start = headingIndex === -1 ? 0 : headingIndex;
  const separatorIndex = lines.findIndex((line, index) => index >= start && /^\|\s*-+/.test(line.trim()));

  if (separatorIndex === -1) {
    return ensureTrailingNewline(`${text.trimEnd()}\n${row}`);
  }

  lines.splice(separatorIndex + 1, 0, row);
  return lines.join("\n");
}

export function insertAfterTableHeader(text, row) {
  const lines = text.split(/\r?\n/);
  const separatorIndex = lines.findIndex((line) => /^\|\s*-+/.test(line.trim()));
  if (separatorIndex === -1) {
    return ensureTrailingNewline(`${text.trimEnd()}\n${row}`);
  }

  lines.splice(separatorIndex + 1, 0, row);
  return lines.join("\n");
}

export function insertAfterHeading(text, heading, entry) {
  const lines = text.split(/\r?\n/);
  const headingIndex = lines.findIndex((line) => line.trim() === heading);
  if (headingIndex === -1) {
    return ensureTrailingNewline(`${text.trimEnd()}\n\n${heading}\n\n${entry}`);
  }

  let insertAt = headingIndex + 1;
  while (insertAt < lines.length && lines[insertAt].trim() === "") {
    insertAt += 1;
  }

  lines.splice(insertAt, 0, entry, "");
  return lines.join("\n");
}

export function removeTaskRows(text, id) {
  return text
    .split(/\r?\n/)
    .filter((line) => !line.includes(id))
    .join("\n");
}

export function componentFromArea(area) {
  const map = {
    BE: "backend",
    FE: "frontend",
    DB: "database",
    INF: "infrastructure",
    DOC: "documentation",
    MCP: "documentation",
    QA: "quality",
  };

  return map[area] ?? undefined;
}

export function simpleDiff(before, after, filePath, action) {
  if (action === "delete") {
    return `--- ${filePath}\n+++ /dev/null\n-${before.split(/\r?\n/).slice(0, 40).join("\n-")}`;
  }

  if (!before) {
    return `--- /dev/null\n+++ ${filePath}\n+${after.split(/\r?\n/).slice(0, 60).join("\n+")}`;
  }

  const beforeLines = before.split(/\r?\n/);
  const afterLines = after.split(/\r?\n/);
  const prefix = commonPrefixLength(beforeLines, afterLines);
  const suffix = commonSuffixLength(beforeLines.slice(prefix), afterLines.slice(prefix));
  const removed = beforeLines.slice(prefix, beforeLines.length - suffix).slice(0, 40);
  const added = afterLines.slice(prefix, afterLines.length - suffix).slice(0, 60);

  return [
    `--- ${filePath}`,
    `+++ ${filePath}`,
    `@@ line ${prefix + 1} @@`,
    ...removed.map((line) => `-${line}`),
    ...added.map((line) => `+${line}`),
  ].join("\n");
}

export function ensureTrailingNewline(text) {
  return text.endsWith("\n") ? text : `${text}\n`;
}

/**
 * Marca una tarea como completada en ia/03_plan.md.
 * Reemplaza | ⏳ TASK-ID | por | ✅ | en la fila correspondiente.
 * Si no quedan filas ⏳ en la fase, actualiza el encabezado de fase a ✅ Completada.
 */
export function markPlanTaskDone(text, taskId) {
  const lines = text.split(/\r?\n/);

  // Buscar la línea que contiene el task ID
  let taskLineIndex = -1;
  for (let i = 0; i < lines.length; i++) {
    if (lines[i].trim().startsWith('|') && lines[i].includes('⏳') && lines[i].includes(taskId)) {
      taskLineIndex = i;
      break;
    }
  }
  if (taskLineIndex === -1) return text;

  // Reemplazar | ⏳ TASK-XYZ | por | ✅ |
  lines[taskLineIndex] = lines[taskLineIndex].replace(/\|\s*⏳\s*[A-Z0-9-]+\s*\|/, '| ✅ |');

  // Buscar el encabezado de fase (hacia atrás: ### Fase N)
  let phaseHeaderIndex = -1;
  for (let i = taskLineIndex - 1; i >= 0; i--) {
    if (/^#{1,4}\s+Fase\s+\d+/.test(lines[i])) {
      phaseHeaderIndex = i;
      break;
    }
  }

  if (phaseHeaderIndex !== -1) {
    // Determinar el final de la sección de fase
    const phaseLevel = (lines[phaseHeaderIndex].match(/^(#+)/) ?? ['', '###'])[1].length;
    let phaseEndIndex = lines.length;
    for (let i = phaseHeaderIndex + 1; i < lines.length; i++) {
      const levelMatch = lines[i].match(/^(#+)\s/);
      if (levelMatch && levelMatch[1].length <= phaseLevel) {
        phaseEndIndex = i;
        break;
      }
    }

    // Si no quedan tareas pendientes en la fase, marcar como completada
    const phaseBody = lines.slice(phaseHeaderIndex + 1, phaseEndIndex);
    const hasPending = phaseBody.some((line) => /⏳/.test(line));
    if (!hasPending) {
      lines[phaseHeaderIndex] = lines[phaseHeaderIndex].replace(/\s*(?:🔄|⏳).*$/, ' ✅ Completada');
    }
  }

  return lines.join('\n');
}

function commonPrefixLength(a, b) {
  let index = 0;
  while (index < a.length && index < b.length && a[index] === b[index]) {
    index += 1;
  }

  return index;
}

function commonSuffixLength(a, b) {
  let index = 0;
  while (index < a.length && index < b.length && a[a.length - 1 - index] === b[b.length - 1 - index]) {
    index += 1;
  }

  return index;
}
