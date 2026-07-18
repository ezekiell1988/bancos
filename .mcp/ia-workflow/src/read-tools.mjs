import fs from "node:fs/promises";
import path from "node:path";
import { requiredDirs, requiredFiles, intentFiles } from "./constants.mjs";
import {
  clampInteger,
  compactMode,
  escapeRegExp,
  localePathSort,
  normalizeSearch,
  requireString,
  rpcError,
  sanitizeAdrId,
  sanitizeIssueId,
  sanitizeTaskId,
} from "./common.mjs";
import {
  firstMarkdownTitle,
  formatTextPayload,
  guidanceForIntent,
  lineContext,
  resourceDescription,
  resourceTitle,
} from "./markdown.mjs";

export function createReadTools({ iaRoot, iaFs, scanForPotentialSecrets }) {
  const { exists, fromIaUri, listMarkdownFiles, normalizeRelativePath, optionalText, readText, safeIaPath, toIaUri } = iaFs;

  async function getContext(input) {
    const intent = requireString(input.intent, "intent");
    if (!Object.hasOwn(intentFiles, intent)) throw rpcError(-32602, `Intención no soportada: ${intent}`);
    const mode = compactMode(input.mode ?? "full");
    const includeText = mode !== "pathsOnly" && input.includeText !== false;
    const maxChars = clampInteger(input.maxChars ?? 12000, 0, 50000);
    const paths = [...intentFiles[intent]];
    if (input.taskId) paths.push(`04_tasks/tasks/${sanitizeTaskId(input.taskId)}.md`);
    if (input.issueId) paths.push(`07_issues/open/${sanitizeIssueId(input.issueId)}.md`);
    const files = [];
    const missing = [];
    for (const relativePath of [...new Set(paths)]) {
      const absolutePath = safeIaPath(relativePath);
      if (await exists(absolutePath)) {
        const text = includeText ? await readText(absolutePath) : undefined;
        files.push({ path: relativePath, uri: toIaUri(relativePath), ...(includeText ? formatTextPayload(text, { mode, maxChars }) : {}) });
      } else missing.push(relativePath);
    }
    return { iaRoot, intent, mode, guidance: guidanceForIntent(intent), files, missing };
  }

  async function listTasks(input) {
    const status = input.status ?? "active";
    const mode = compactMode(input.mode ?? "summary");
    if (!["active", "backlog", "blocked", "done", "all"].includes(status)) throw rpcError(-32602, `Estado no soportado: ${status}`);
    const groups = {};
    if (["active", "all"].includes(status)) groups.active = { path: "04_tasks/current.md", ...(mode === "pathsOnly" ? {} : { entries: await readTableLikeEntries("04_tasks/current.md") }), files: await listTaskFiles("04_tasks/tasks") };
    if (["backlog", "all"].includes(status)) groups.backlog = { path: "04_tasks/backlog.md", ...(mode === "pathsOnly" ? {} : { entries: await readBulletEntries("04_tasks/backlog.md") }) };
    if (["blocked", "all"].includes(status)) groups.blocked = { path: "04_tasks/blocked.md", ...(mode === "pathsOnly" ? {} : { entries: await readBulletEntries("04_tasks/blocked.md") }) };
    if (["done", "all"].includes(status)) groups.done = { path: "04_tasks/done", files: await listDoneFiles() };
    return { status, mode, groups };
  }

  async function readTask(input) {
    const id = sanitizeTaskId(requireString(input.id, "id"));
    const mode = compactMode(input.mode ?? "full");
    const maxChars = clampInteger(input.maxChars ?? 12000, 0, 50000);
    const relativePath = `04_tasks/tasks/${id}.md`;
    if (await exists(safeIaPath(relativePath))) {
      return { id, path: relativePath, uri: toIaUri(relativePath), ...formatTextPayload(await readText(safeIaPath(relativePath)), { mode, maxChars }) };
    }
    const doneFiles = (await listMarkdownFiles(safeIaPath("04_tasks/done"), "04_tasks/done")).sort((a, b) => b.localeCompare(a));
    const sectionRe = new RegExp(`(#{1,3}\\s+${escapeRegExp(id)}[\\s\\S]*?)(?=\\n#{1,3}\\s|$)`, "i");
    for (const donePath of doneFiles) {
      const text = await readText(safeIaPath(donePath));
      const match = text.match(sectionRe);
      if (match) {
        return { id, path: donePath, uri: toIaUri(donePath), archived: true, archivedIn: donePath, ...formatTextPayload(match[1].trim(), { mode, maxChars }) };
      }
    }
    throw rpcError(-32602, `No existe la tarea ${id} (activa ni archivada)`);
  }

  async function listDecisions(input) {
    const query = normalizeSearch(input.query ?? "");
    const mode = compactMode(input.mode ?? "summary");
    const files = await listMarkdownFiles(safeIaPath("06_decisions"), "06_decisions");
    const decisions = [];
    for (const file of files) {
      const text = await readText(safeIaPath(file));
      const title = firstMarkdownTitle(text) ?? path.basename(file, ".md");
      const id = path.basename(file).match(/^(ADR-\d+)/)?.[1] ?? path.basename(file, ".md");
      if (!query || normalizeSearch(`${id} ${title} ${file}`).includes(query)) decisions.push({ id, title, path: file, uri: toIaUri(file) });
    }
    return { mode, count: decisions.length, decisions };
  }

  async function readDecision(input) {
    const id = sanitizeAdrId(requireString(input.id, "id"));
    const mode = compactMode(input.mode ?? "full");
    const maxChars = clampInteger(input.maxChars ?? 12000, 0, 50000);
    const decisions = await listDecisions({ query: id });
    const match = decisions.decisions.find((decision) => decision.id.toLowerCase() === id.toLowerCase());
    if (!match) throw rpcError(-32602, `No existe el ADR ${id}`);
    return { ...match, ...formatTextPayload(await readText(safeIaPath(match.path)), { mode, maxChars }) };
  }

  async function listIssues(input) {
    const mode = compactMode(input.mode ?? "summary");
    const maxChars = clampInteger(input.maxChars ?? 8000, 0, 50000);
    const includeText = mode === "full" || input.includeText === true;
    const issueFiles = await listMarkdownFiles(safeIaPath("07_issues/open"), "07_issues/open");
    const issues = [];
    for (const file of issueFiles.filter((item) => path.basename(item) !== "README.md")) {
      const text = await readText(safeIaPath(file));
      issues.push({ id: path.basename(file, ".md"), title: firstMarkdownTitle(text) ?? path.basename(file, ".md"), path: file, uri: toIaUri(file), ...(includeText ? formatTextPayload(text, { mode, maxChars }) : {}) });
    }
    const currentPath = "07_issues/current.md";
    const current = (await exists(safeIaPath(currentPath))) ? { path: currentPath, uri: toIaUri(currentPath), ...(includeText ? formatTextPayload(await readText(safeIaPath(currentPath)), { mode, maxChars }) : {}) } : null;
    return { mode, current, count: issues.length, issues };
  }

  async function searchIa(input) {
    const query = normalizeSearch(requireString(input.query, "query"));
    const scope = input.scope ?? "all";
    const maxResults = clampInteger(input.maxResults ?? 20, 1, 100);
    const contextLines = clampInteger(input.contextLines ?? 0, 0, 5);
    const files = [];
    for (const root of scopeRoots(scope)) {
      const absolute = safeIaPath(root);
      if (!(await exists(absolute))) continue;
      const stat = await fs.stat(absolute);
      if (stat.isDirectory()) files.push(...(await listMarkdownFiles(absolute, root === "." ? "" : root)));
      else if (root.endsWith(".md")) files.push(root);
    }
    const results = [];
    for (const relativePath of [...new Set(files)]) {
      const lines = (await readText(safeIaPath(relativePath))).split(/\r?\n/);
      for (let index = 0; index < lines.length; index += 1) {
        if (normalizeSearch(lines[index]).includes(query)) {
          results.push({ path: relativePath, uri: toIaUri(relativePath), line: index + 1, excerpt: lines[index].trim(), ...(contextLines > 0 ? { context: lineContext(lines, index, contextLines) } : {}) });
          if (results.length >= maxResults) return { query: input.query, scope, count: results.length, results };
        }
      }
    }
    return { query: input.query, scope, count: results.length, results };
  }

  async function validateIa() {
    const missingFiles = [];
    const missingDirs = [];
    for (const file of requiredFiles) if (!(await exists(safeIaPath(file)))) missingFiles.push(file);
    for (const dir of requiredDirs) {
      const absolute = safeIaPath(dir);
      if (!(await exists(absolute)) || !(await fs.stat(absolute)).isDirectory()) missingDirs.push(dir);
    }
    const warnings = [];
    const errors = [];
    const readmeText = await optionalText("README.md");
    if (readmeText && readmeText.length > 12000) warnings.push("README.md parece grande; debe ser router.");
    const decisionIndex = await optionalText("06_decisions.md");
    const decisionFiles = await listMarkdownFiles(safeIaPath("06_decisions"), "06_decisions");
    if (decisionIndex && decisionFiles.length === 0) warnings.push("Faltan ADRs individuales.");
    for (const finding of await scanForPotentialSecrets()) warnings.push(`Posible secret en ${finding.path}:${finding.line} (${finding.pattern})`);
    const activeTasks = await listMarkdownFiles(safeIaPath("04_tasks/tasks"), "04_tasks/tasks");
    for (const taskPath of activeTasks) {
      const taskText = await readText(safeIaPath(taskPath));
      if (!/^# TASK-[A-Z]{1,3}-[A-Z]{2,4}-\d{2,}/m.test(taskText)) warnings.push(`Task con formato inesperado: ${taskPath}`);
    }
    const sizeChecks = [
      { file: "00_context.md", limit: 20000, action: "mover secciones históricas o estables a archivos de soporte y conservar solo las constantes del proyecto" },
      { file: "01_requirements.md", limit: 24000, action: "dividir por feature/área en 01_requirements/{feature-o-area}.md y dejar este archivo como índice" },
      { file: "02_architecture.md", limit: 24000, action: "separar el detalle por dominio en 02_architecture/{dominio}.md y convertir el archivo raíz en índice compacto" },
      { file: "03_plan.md", limit: 20000, action: "archivar fases completadas en 03_plan/historial.md y conservar solo fases activas o próximas" },
    ];
    for (const { file, limit, action } of sizeChecks) {
      const text = await optionalText(file);
      if (text && text.length > limit) warnings.push(`${file} tiene ${text.length} chars (> ${limit}): ${action}.`);
    }
    const progressCurrent = await optionalText("05_progress/current.md");
    if (progressCurrent && progressCurrent.length > 12000) warnings.push(`05_progress/current.md tiene ${progressCurrent.length} chars (> 12 000): ejecutar archive_progress para mover entradas antiguas a 05_progress/archive/.`);
    return { iaRoot, valid: missingFiles.length === 0 && missingDirs.length === 0 && errors.length === 0, missingFiles, missingDirs, errors, warnings };
  }

  async function readFile(input) {
    const relativePath = normalizeRelativePath(requireString(input.path, "path"));
    const mode = compactMode(input.mode ?? "full");
    const maxChars = clampInteger(input.maxChars ?? 12000, 0, 50000);
    if (!(await exists(safeIaPath(relativePath)))) throw rpcError(-32602, `No existe el archivo ${relativePath}`);
    return { path: relativePath, uri: toIaUri(relativePath), ...formatTextPayload(await readText(safeIaPath(relativePath)), { mode, maxChars }) };
  }

  async function listResources() {
    const files = await listMarkdownFiles(iaRoot, "");
    return files.sort(localePathSort).map((relativePath) => ({ uri: toIaUri(relativePath), name: path.basename(relativePath), title: resourceTitle(relativePath), description: resourceDescription(relativePath), mimeType: "text/markdown" }));
  }

  async function readResource(params) {
    const uri = requireString(params.uri, "uri");
    const relativePath = fromIaUri(uri);
    if (!(await exists(safeIaPath(relativePath)))) throw rpcError(-32602, `No existe el recurso ${uri}`);
    return { contents: [{ uri: toIaUri(relativePath), mimeType: "text/markdown", text: await readText(safeIaPath(relativePath)) }] };
  }

  async function listTaskFiles(relativeDir) {
    const files = await listMarkdownFiles(safeIaPath(relativeDir), relativeDir);
    const tasks = [];
    for (const file of files) {
      const text = await readText(safeIaPath(file));
      tasks.push({ id: path.basename(file, ".md"), title: firstMarkdownTitle(text) ?? path.basename(file, ".md"), path: file, uri: toIaUri(file) });
    }
    return tasks.sort((a, b) => a.id.localeCompare(b.id));
  }

  async function listDoneFiles() {
    const files = await listMarkdownFiles(safeIaPath("04_tasks/done"), "04_tasks/done");
    return files.sort(localePathSort).map((file) => ({ path: file, uri: toIaUri(file) }));
  }

  async function readTableLikeEntries(relativePath) {
    return (await optionalText(relativePath)).split(/\r?\n/).map((line) => line.trim()).filter((line) => line.startsWith("|") && !line.includes("---")).slice(1);
  }

  async function readBulletEntries(relativePath) {
    return (await optionalText(relativePath)).split(/\r?\n/).map((line) => line.trim()).filter((line) => line.startsWith("* ") || line.startsWith("- ")).map((line) => line.slice(2).trim());
  }

  return { getContext, listTasks, readTask, listDecisions, readDecision, listIssues, searchIa, validateIa, readFile, listResources, readResource };
}

function scopeRoots(scope) {
  const roots = {
    all: ["."],
    tasks: ["04_tasks.md", "04_tasks"],
    decisions: ["06_decisions.md", "06_decisions"],
    issues: ["07_issues.md", "07_issues"],
    progress: ["05_progress.md", "05_progress"],
    context: ["README.md", "00_context.md", "01_requirements.md", "02_architecture.md", "03_plan.md"],
  };
  if (!roots[scope]) throw rpcError(-32602, `Scope no soportado: ${scope}`);
  return roots[scope];
}
