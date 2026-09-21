// Puerto de las primitivas de filesystem de WorkflowService.cs: lectura confinada a
// /ia, enumeración de Markdown, resolución de tareas/issues y el commit seguro
// (preview por defecto, write atómico solo con apply:true). No acepta rutas fuera de /ia.
import { existsSync, mkdirSync, readFileSync, readdirSync, renameSync, statSync, unlinkSync, writeFileSync } from 'node:fs';
import { randomUUID } from 'node:crypto';
import path from 'node:path';
import { ToolError } from '../../_shared/results.mjs';
import { escapeRegExp, secretPatternMatches } from './text.mjs';

const WRITABLE_ROOTS = [
  '02_architecture.md',
  '03_plan.md',
  '03_plan/',
  '04_tasks/current.md',
  '04_tasks/blocked.md',
  '04_tasks/tasks/',
  '04_tasks/done/',
  '05_progress/current.md',
  '05_progress/by-component/',
  '05_progress/archive/',
  '06_decisions.md',
  '06_decisions/',
  '07_issues/current.md',
  '07_issues/open/',
  '07_issues/archive/',
  '08_retrospective.md',
];

const REQUIRED_FILES = [
  '00_context.md', '01_requirements.md', '02_architecture.md', '03_plan.md', '04_tasks.md',
  '04_tasks/current.md', '04_tasks/blocked.md', '05_progress.md', '05_progress/current.md',
  '06_decisions.md', '07_issues.md', '07_issues/current.md', '08_retrospective.md',
];

const REQUIRED_DIRECTORIES = [
  '04_tasks/tasks', '04_tasks/done', '05_progress/archive', '06_decisions', '07_issues/open', '07_issues/archive',
];

/** Construye un OperationResult omitiendo claves null/undefined (igual que
 * JsonIgnoreCondition.WhenWritingNull en JsonDefaults.Compact del lado .NET). */
export function opResult(fields) {
  const out = {};
  for (const [key, value] of Object.entries(fields)) {
    if (value !== null && value !== undefined) out[key] = value;
  }
  return out;
}

export function normalizeRelative(relative) {
  if (!relative || relative.trim() === '' || path.isAbsolute(relative)) {
    throw new ToolError('La ruta debe ser relativa dentro de /ia.');
  }
  const normalized = relative.replace(/\\/g, '/').replace(/^\/+/, '');
  if (normalized.split('/').some((part) => part === '' || part === '.' || part === '..')) {
    throw new ToolError('La ruta está fuera de /ia.');
  }
  return normalized;
}

/** Crea un store de lectura/escritura confinado a `iaRoot` (closure, sin estado compartido entre servidores). */
export function createStore(iaRoot) {
  const root = path.resolve(iaRoot);
  const rootWithSep = root.endsWith(path.sep) ? root : root + path.sep;

  function pathFor(relative) {
    const normalized = normalizeRelative(relative);
    const candidate = path.resolve(root, normalized);
    if (!candidate.startsWith(rootWithSep) && candidate !== root) throw new ToolError('La ruta está fuera de /ia.');
    return candidate;
  }

  function pathForDirectory(relative) {
    if (!relative || relative.trim() === '' || path.isAbsolute(relative)) throw new ToolError('Directorio fuera de /ia.');
    const candidate = path.resolve(root, relative);
    if (!candidate.startsWith(rootWithSep) && candidate !== root) throw new ToolError('Directorio fuera de /ia.');
    return candidate;
  }

  function relative(fullPath) {
    return path.relative(root, fullPath).split(path.sep).join('/');
  }

  function read(rel) {
    const p = pathFor(rel);
    if (!existsSync(p)) throw new ToolError(`No existe ${rel}.`);
    return readFileSync(p, 'utf8');
  }

  function readOptional(rel) {
    const p = pathFor(rel);
    return existsSync(p) ? readFileSync(p, 'utf8') : '';
  }

  function tryRead(rel) {
    const p = pathFor(rel);
    return existsSync(p) ? { ok: true, content: readFileSync(p, 'utf8') } : { ok: false, content: '' };
  }

  function enumerateMarkdown(relativeRoot) {
    const dir = relativeRoot === '.' ? root : pathForDirectory(relativeRoot);
    if (!existsSync(dir)) return [];
    const out = [];
    const walk = (current) => {
      for (const entry of readdirSync(current, { withFileTypes: true })) {
        const full = path.join(current, entry.name);
        if (entry.isDirectory()) walk(full);
        else if (entry.isFile() && entry.name.endsWith('.md')) out.push(full);
      }
    };
    walk(dir);
    return out;
  }

  function resolveTask(id, includeArchived) {
    if (!/^TASK-[A-Z0-9]{1,3}-[A-Z]+-\d+$/i.test(id)) throw new ToolError('TASK-ID inválido.');
    const active = `04_tasks/tasks/${id}.md`;
    if (existsSync(pathFor(active))) return { relativePath: active, archived: false };
    if (includeArchived) {
      const done = enumerateMarkdown('04_tasks/done').sort((a, b) => path.basename(b).localeCompare(path.basename(a)));
      const headingTest = new RegExp(`^#\\s+${escapeRegExp(id)}\\s+—`, 'm');
      for (const file of done) {
        if (headingTest.test(readFileSync(file, 'utf8'))) return { relativePath: relative(file), archived: true };
      }
    }
    throw new ToolError(`No existe la tarea ${id}.`);
  }

  function resolveIssue(id) {
    const openDir = pathForDirectory('07_issues/open');
    if (existsSync(openDir)) {
      const match = readdirSync(openDir).find((name) => name.startsWith(`${id}-`) && name.endsWith('.md'));
      if (match) return { relativePath: relative(path.join(openDir, match)), archived: false };
    }
    for (const file of enumerateMarkdown('07_issues/archive')) {
      if (readFileSync(file, 'utf8').includes(`# ${id} `)) return { relativePath: relative(file), archived: true };
    }
    throw new ToolError(`No existe el issue ${id}.`);
  }

  function resolveOpenIssue(id) {
    const issue = resolveIssue(id);
    if (issue.archived) throw new ToolError(`${id} no está abierto.`);
    return issue;
  }

  function nextNumber(prefix, relativeRoot, width) {
    let maximum = 0;
    const pattern = new RegExp(escapeRegExp(prefix) + '(\\d+)', 'g');
    for (const file of enumerateMarkdown(relativeRoot)) {
      const text = readFileSync(file, 'utf8');
      for (const m of text.matchAll(pattern)) {
        const value = parseInt(m[1], 10);
        if (!Number.isNaN(value)) maximum = Math.max(maximum, value);
      }
    }
    return String(maximum + 1).padStart(width, '0');
  }

  function nextTaskId(taskInitials, area) {
    const pattern = new RegExp(`TASK-${escapeRegExp(taskInitials)}-${escapeRegExp(area)}-(\\d+)`, 'g');
    let max = 0;
    for (const file of enumerateMarkdown('04_tasks')) {
      const text = readFileSync(file, 'utf8');
      for (const m of text.matchAll(pattern)) max = Math.max(max, parseInt(m[1], 10));
    }
    return `TASK-${taskInitials}-${area}-${String(max + 1).padStart(2, '0')}`;
  }

  function validateWritable(relativePath, content) {
    const normalized = normalizeRelative(relativePath);
    const allowed = WRITABLE_ROOTS.some(
      (allowedRoot) =>
        normalized.toLowerCase() === allowedRoot.toLowerCase() ||
        (allowedRoot.endsWith('/') && normalized.toLowerCase().startsWith(allowedRoot.toLowerCase())),
    );
    if (!allowed) throw new ToolError('Ruta no permitida para escritura: ' + normalized);
    if (secretPatternMatches(content)) throw new ToolError('El cambio planeado parece contener un secreto; no se aplicó.');
  }

  function validate() {
    const missingFiles = REQUIRED_FILES.filter((f) => !existsSync(pathFor(f)));
    const missingDirectories = REQUIRED_DIRECTORIES.filter((d) => !existsSync(pathForDirectory(d)));
    const errors = [];
    const warnings = [];
    if (missingFiles.length > 0) errors.push(`Faltan archivos requeridos: ${missingFiles.join(', ')}.`);
    if (missingDirectories.length > 0) errors.push(`Faltan directorios requeridos: ${missingDirectories.join(', ')}.`);

    const limits = [
      ['00_context.md', 20_000, 'mover contexto histórico o estable a archivos de soporte'],
      ['01_requirements.md', 24_000, 'dividir por feature/área y dejar un índice'],
      ['02_architecture.md', 24_000, 'separar por dominio y conservar contratos esenciales en el índice'],
      ['03_plan.md', 20_000, 'archivar fases completadas en 03_plan/'],
    ];
    for (const [rel, limit, remedy] of limits) {
      const result = tryRead(rel);
      if (result.ok && result.content.length > limit) {
        warnings.push(`${rel} tiene ${result.content.length} caracteres (límite ${limit}); acción: ${remedy}.`);
      }
    }

    for (const file of enumerateMarkdown('04_tasks/tasks')) {
      const text = readFileSync(file, 'utf8');
      if (text.includes('**schemaVersion:** 2') && !text.includes('## Historial de eventos V2')) {
        errors.push(`${relative(file)} declara schemaVersion 2 sin historial JSONL.`);
      }
    }

    return { valid: errors.length === 0, errors, warnings, missingFiles, missingDirectories };
  }

  /**
   * Aplica (o previsualiza) un conjunto de cambios de forma atómica: escribe a un
   * archivo temporal y renombra, igual que File.Move(overwrite:true) en .NET.
   */
  function commit(operation, apply, planned, id, deleteAfter = []) {
    const changes = Object.keys(planned).map((rel) => ({
      path: rel,
      action: existsSync(pathFor(rel)) ? 'update' : 'create',
    }));
    for (const rel of deleteAfter) changes.push({ path: rel, action: 'archive/remove active copy' });
    for (const rel of Object.keys(planned)) validateWritable(rel, planned[rel]);

    if (!apply) {
      return opResult({ preview: true, applied: false, message: `Preview de ${operation}; no se modificó ningún archivo.`, changes, validation: validate(), id });
    }

    for (const [rel, content] of Object.entries(planned)) {
      const full = pathFor(rel);
      mkdirSync(path.dirname(full), { recursive: true });
      const temp = `${full}.mcp-tmp-${randomUUID().replace(/-/g, '')}`;
      writeFileSync(temp, content, 'utf8');
      renameSync(temp, full);
    }
    for (const rel of deleteAfter) {
      const full = pathFor(rel);
      if (existsSync(full)) unlinkSync(full);
    }

    const validation = validate();
    return opResult({ preview: false, applied: true, message: `${operation} aplicado.`, changes, validation, id });
  }

  return {
    root,
    read,
    readOptional,
    tryRead,
    pathFor,
    pathForDirectory,
    relative,
    enumerateMarkdown,
    resolveTask,
    resolveIssue,
    resolveOpenIssue,
    nextNumber,
    nextTaskId,
    validate,
    commit,
    statSync: (rel) => statSync(pathFor(rel)),
  };
}
