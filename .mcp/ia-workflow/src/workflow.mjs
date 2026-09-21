// Puerto de la lógica de negocio de WorkflowService.cs: contexto por intención,
// inspección de solo lectura, y las 19 operaciones de escritura (preview por
// defecto, apply:true para mutar). Usa store.mjs para IO y markdown.mjs/text.mjs
// para las transformaciones de contenido.
import { readFileSync } from 'node:fs';
import path from 'node:path';
import { randomUUID } from 'node:crypto';
import { ToolError } from '../../_shared/results.mjs';
import { createStore, normalizeRelative, opResult } from './store.mjs';
import {
  appendEvent,
  bullets,
  ensureStatus,
  field,
  initials,
  matchesTaskGroup,
  normalizeInitials,
  normalizeRisk,
  numbered,
  require_,
  requireChoice,
  setField,
  slug,
  status,
  summary,
  taskId as taskIdOf,
  timestamp,
  truncate,
} from './text.mjs';
import {
  addBulletUnderHeading,
  addIndexLine,
  addProgress,
  addReviewFlag,
  extractArchivedTask,
  independentPlanRelativePath,
  insertPlanRow,
  insertTaskRow,
  markPlanTaskComplete,
  moveTaskRow,
  phaseSection,
  removeIndexLine,
  removeTaskRow,
  updateIndependentPlanPhase,
  updateIndependentPlanStatus,
  updateIndependentPlanTask,
} from './markdown.mjs';

const INTENT_SELECTION = {
  planificar: ['00_context.md', '01_requirements.md', '02_architecture.md', '03_plan.md', '04_tasks/current.md', '04_tasks/blocked.md', '05_progress/current.md'],
  implementar: ['00_context.md', '01_requirements.md', '02_architecture.md', '03_plan.md', '04_tasks/current.md', '05_progress/current.md', '07_issues/current.md'],
  revisar: ['00_context.md', '01_requirements.md', '02_architecture.md', '03_plan.md', '06_decisions.md', '07_issues/current.md'],
  depurar: ['00_context.md', '01_requirements.md', '02_architecture.md', '04_tasks/current.md', '05_progress/current.md', '07_issues/current.md'],
  cerrar_sesion: ['00_context.md', '02_architecture.md', '03_plan.md', '04_tasks/current.md', '04_tasks/blocked.md', '05_progress/current.md', '07_issues/current.md', '08_retrospective.md'],
};

function clampInt(value, min, max, fallback) {
  const n = Number(value ?? fallback);
  return Number.isFinite(n) ? Math.min(Math.max(Math.trunc(n), min), max) : fallback;
}

export function createWorkflow(iaRoot) {
  const store = createStore(iaRoot);

  // ── ia_validate ────────────────────────────────────────────────────────────
  function validate() {
    return store.validate();
  }

  // ── ia_get_context ────────────────────────────────────────────────────────
  function getContext(intent, taskId, issueId, mode, includeText, maxChars) {
    const normalizedIntent = requireChoice(intent, ['planificar', 'implementar', 'revisar', 'depurar', 'cerrar_sesion'], 'intent');
    const normalizedMode = mode ?? 'full';
    requireChoice(normalizedMode, ['pathsOnly', 'summary', 'full'], 'mode');
    const selection = [...INTENT_SELECTION[normalizedIntent]];
    if (taskId && taskId.trim() !== '') selection.push(store.resolveTask(taskId, true).relativePath);
    if (issueId && issueId.trim() !== '') selection.push(store.resolveIssue(issueId).relativePath);
    const wantsText = includeText ?? normalizedMode !== 'pathsOnly';
    const cap = clampInt(maxChars, 256, 50_000, 8_000);
    const seen = new Set();
    const files = [];
    for (const rel of selection) {
      const key = rel.toLowerCase();
      if (seen.has(key)) continue;
      seen.add(key);
      files.push(readContextFile(rel, normalizedMode, wantsText, cap));
    }
    return { intent: normalizedIntent, mode: normalizedMode, files };
  }

  function readContextFile(relative, mode, includeText, cap) {
    const text = store.read(relative);
    if (mode === 'pathsOnly' || !includeText) return { path: relative };
    if (mode === 'summary') return { path: relative, summary: summary(text) };
    const { text: truncated, truncated: didTruncate } = truncate(text, cap);
    return { path: relative, text: truncated, truncated: didTruncate };
  }

  // ── ia_inspect ────────────────────────────────────────────────────────────
  function inspect(request) {
    const action = requireChoice(
      request.action,
      ['list_tasks', 'read_task', 'list_decisions', 'read_decision', 'list_issues', 'list_pending_devops_link', 'read_file', 'search', 'metrics', 'migration'],
      'action',
    );
    ensureInspectArguments(action, request);
    switch (action) {
      case 'list_tasks':
        return listTasks(request.status, request.mode, request.user);
      case 'read_task':
        return readTask(require_(request.id, 'id'), request.mode, request.maxChars);
      case 'list_decisions':
        return listDecisions(request.query, request.mode);
      case 'read_decision':
        return readDecision(require_(request.id, 'id'), request.mode, request.maxChars);
      case 'list_issues':
        return listIssues(request.mode, request.includeText, request.maxChars);
      case 'list_pending_devops_link':
        return listPendingDevOpsLinks();
      case 'read_file':
        return readFile(require_(request.path, 'path'), request.mode, request.maxChars);
      case 'search':
        return search(require_(request.query, 'query'), request.scope, request.maxResults, request.contextLines);
      case 'metrics':
        return metrics(request.filter, request.targetCount, request.seed);
      default:
        return migrationPreview();
    }
  }

  function ensureInspectArguments(action, request) {
    const num = (v) => (v === null || v === undefined ? null : String(v));
    const groups = {
      list_tasks: [request.status, request.mode, request.user],
      read_task: [request.id, request.mode, num(request.maxChars)],
      list_decisions: [request.query, request.mode],
      read_decision: [request.id, request.mode, num(request.maxChars)],
      list_issues: [request.mode, request.includeText === undefined || request.includeText === null ? null : String(request.includeText), num(request.maxChars)],
      read_file: [request.path, request.mode, num(request.maxChars)],
      search: [request.query, request.scope, num(request.maxResults), num(request.contextLines)],
      metrics: [request.from, request.asOf, request.filter, num(request.targetCount), num(request.seed)],
    };
    const allowed = groups[action] ?? [];
    const all = [
      request.status, request.mode, request.user, request.id, num(request.maxChars), request.query,
      request.includeText === undefined || request.includeText === null ? null : String(request.includeText),
      request.path, request.scope, num(request.maxResults), num(request.contextLines), request.from, request.asOf,
      request.filter, num(request.targetCount), num(request.seed),
    ];
    const countDefined = (arr) => arr.filter((v) => v !== null && v !== undefined).length;
    if (countDefined(all) > countDefined(allowed)) {
      throw new ToolError(`${action} no acepta uno o más parámetros de otra acción.`);
    }
  }

  function listTasks(statusFilter, mode, user) {
    const normalizedStatus = statusFilter ?? 'all';
    requireChoice(normalizedStatus, ['active', 'backlog', 'blocked', 'done', 'all'], 'status');
    const sources = { active: ['04_tasks/tasks'], backlog: ['04_tasks/tasks'], blocked: ['04_tasks/tasks'], done: ['04_tasks/done'] };
    const groupNames = normalizedStatus === 'all' ? Object.keys(sources) : [normalizedStatus];
    const groups = {};
    for (const group of groupNames) {
      const files = sources[group]
        .flatMap((root) => store.enumerateMarkdown(root))
        .map((full) => ({ path: store.relative(full), text: readFileSync(full, 'utf8') }))
        .filter((item) => matchesTaskGroup(item.text, group))
        .filter((item) => !user || path.basename(item.path, '.md').toUpperCase().startsWith(`TASK-${user.toUpperCase()}-`))
        .map((item) => ({ id: taskIdOf(item.text), path: item.path, status: status(item.text), summary: summary(item.text) }));
      groups[group] = { path: group === 'done' ? '04_tasks/done' : '04_tasks/current.md', files };
    }
    return { status: normalizedStatus, mode: mode ?? 'summary', user: user ?? null, groups };
  }

  function readTask(id, mode, maxChars) {
    const task = store.resolveTask(id, true);
    const text = task.archived ? extractArchivedTask(store.read(task.relativePath), id) : store.read(task.relativePath);
    return fileView(task.relativePath, text, mode, maxChars, task.archived, id);
  }

  function listDecisions(query, mode) {
    const decisions = store
      .enumerateMarkdown('06_decisions')
      .map((full) => ({ path: store.relative(full), text: readFileSync(full, 'utf8') }))
      .filter((item) => item.path.toLowerCase() !== '06_decisions.md')
      .filter((item) => !query || item.text.toLowerCase().includes(query.toLowerCase()))
      .map((item) => ({ id: (/^#\s+(ADR-\d+)/m.exec(item.text) ?? [, ''])[1], path: item.path, summary: summary(item.text) }));
    return { mode: mode ?? 'summary', decisions };
  }

  function readDecision(id, mode, maxChars) {
    const file = store.enumerateMarkdown('06_decisions').find((full) => path.basename(full).startsWith(`${id}-`));
    if (!file) throw new ToolError(`No existe ${id}.`);
    return fileView(store.relative(file), readFileSync(file, 'utf8'), mode, maxChars, false, id);
  }

  function listIssues(mode, includeText, maxChars) {
    const issues = store
      .enumerateMarkdown('07_issues/open')
      .map((full) => ({ path: store.relative(full), text: readFileSync(full, 'utf8') }))
      .map((item) => ({
        id: (/^#\s+(ISSUE-\d+)/m.exec(item.text) ?? [, ''])[1],
        path: item.path,
        status: field(item.text, 'Estado'),
        summary: summary(item.text),
        text: includeText === true ? truncate(item.text, maxChars ?? 8_000).text : null,
      }));
    return { mode: mode ?? 'summary', current: { path: '07_issues/current.md' }, issues };
  }

  function listPendingDevOpsLinks() {
    const pending = store
      .enumerateMarkdown('04_tasks/tasks')
      .map((full) => ({ path: store.relative(full), text: readFileSync(full, 'utf8') }))
      .filter((item) => {
        const devops = field(item.text, 'DevOps');
        return devops === '—' || devops === '' || devops.toLowerCase().includes('pendiente');
      })
      .map((item) => ({ id: taskIdOf(item.text), path: item.path }));
    return { pending };
  }

  function readFile(relative, mode, maxChars) {
    if (!relative.toLowerCase().endsWith('.md')) throw new ToolError('Solo se permite leer Markdown dentro de /ia.');
    const safe = normalizeRelative(relative);
    return fileView(safe, store.read(safe), mode, maxChars, false, null);
  }

  function search(query, scope, maxResults, contextLines) {
    const normalizedScope = scope ?? 'all';
    requireChoice(normalizedScope, ['all', 'tasks', 'decisions', 'issues', 'progress', 'context'], 'scope');
    const roots = { tasks: ['04_tasks'], decisions: ['06_decisions'], issues: ['07_issues'], progress: ['05_progress'], context: ['.'], all: ['.'] }[normalizedScope];
    const results = [];
    const limit = clampInt(maxResults, 1, 100, 20);
    const around = clampInt(contextLines, 0, 5, 1);
    outer: for (const root of roots) {
      for (const file of store.enumerateMarkdown(root)) {
        const lines = readFileSync(file, 'utf8').split('\n');
        for (let i = 0; i < lines.length && results.length < limit; i++) {
          if (!lines[i].toLowerCase().includes(query.toLowerCase())) continue;
          const start = Math.max(0, i - around);
          const end = Math.min(lines.length - 1, i + around);
          results.push({ path: store.relative(file), line: i + 1, text: lines.slice(start, end + 1).join('\n') });
        }
        if (results.length >= limit) break outer;
      }
    }
    return { query, scope: normalizedScope, count: results.length, results };
  }

  function metrics(filter, targetCount, seed) {
    const completed = store.enumerateMarkdown('04_tasks/done').length;
    const active = store.enumerateMarkdown('04_tasks/tasks').length;
    const forecast =
      targetCount === undefined || targetCount === null
        ? { status: 'not_requested' }
        : { status: completed === 0 ? 'insufficient_data' : 'estimated', targetCount, seed: seed ?? 7 };
    return { filter: filter ?? null, sample: { completed, active }, throughput: { completedTasks: completed }, forecast };
  }

  function migrationPreview() {
    const classifications = store.enumerateMarkdown('04_tasks').map((full) => ({
      path: store.relative(full),
      v2: readFileSync(full, 'utf8').includes('**schemaVersion:** 2'),
    }));
    return {
      mode: 'preview',
      writeRequired: true,
      counts: { v1: classifications.filter((c) => !c.v2).length, v2: classifications.filter((c) => c.v2).length },
      classifications,
    };
  }

  function fileView(pathValue, text, mode, maxChars, archived, id) {
    const normalizedMode = mode ?? 'full';
    requireChoice(normalizedMode, ['pathsOnly', 'summary', 'full'], 'mode');
    if (normalizedMode === 'pathsOnly') return { path: pathValue, id: id ?? null, archived };
    if (normalizedMode === 'summary') return { path: pathValue, id: id ?? null, archived, summary: summary(text) };
    const { text: truncated, truncated: didTruncate } = truncate(text, maxChars ?? 8_000);
    return { path: pathValue, id: id ?? null, archived, text: truncated, truncated: didTruncate };
  }

  // ── resources (ia:///...) ────────────────────────────────────────────────
  function readResource(relative) {
    if (!relative.toLowerCase().endsWith('.md')) throw new ToolError('Los resources del workflow solo exponen Markdown.');
    return store.read(normalizeRelative(relative));
  }

  // ── create_task ───────────────────────────────────────────────────────────
  function validateTaskRequest(request) {
    require_(request.title, 'title');
    require_(request.context, 'context');
    require_(request.objective, 'objective');
    require_(request.expectedOutput, 'expectedOutput');
    require_(request.rollback, 'rollback');
    if (
      (request.allowedScope ?? []).length === 0 ||
      (request.outOfScope ?? []).length === 0 ||
      (request.acceptanceCriteria ?? []).length === 0 ||
      (request.technicalPlan ?? []).length === 0 ||
      (request.steps ?? []).length === 0 ||
      (request.validation ?? []).length === 0
    ) {
      throw new ToolError('Los arrays de alcance, aceptación, plan, pasos y validación deben tener al menos un elemento.');
    }
  }

  function buildTaskDocument(id, request, ts) {
    const eventJson = JSON.stringify({
      schemaVersion: 2,
      eventId: randomUUID(),
      taskId: id,
      event: 'created',
      occurredAt: ts,
      actor: 'MCP iaWorkflow',
      reason: 'Tarea creada en Borrador.',
    });
    const author = request.authorName && request.authorName.trim() !== '' ? request.authorName.trim() : '—';
    const authorEmail = request.authorEmail && request.authorEmail.trim() !== '' ? ` \`<${request.authorEmail.trim()}>\`` : '';
    return `# ${id} — ${request.title.trim()}

**schemaVersion:** 2
**Estado:** Borrador
**DevOps:** —
**CRM:** —
**Autor:** ${author}${authorEmail}
**Aprobado por:** —
**Rama:** ${request.branch?.trim() || '—'}
**createdAt:** ${ts}
**approvedAt:** —
**startedAt:** —
**finishedAt:** —
**Área:** ${request.area.toUpperCase()}
**Prioridad:** ${(request.priority || 'media').toLowerCase()}
**Riesgo:** ${normalizeRisk(request.risk)}
**Aprobación:** pendiente
**parentId:** —
**workItemType:** Task
**workstream:** ia-workflow
**tags:** ${request.area.toLowerCase()}, ia-workflow

---

## Título

${request.title.trim()}

## Contexto

${request.context.trim()}

## Objetivo

${request.objective.trim()}

## Alcance permitido

${bullets(request.allowedScope, 'Sin alcance declarado.')}

## Fuera de alcance

${bullets(request.outOfScope, 'Sin exclusiones declaradas.')}

## Criterios de aceptación

${bullets(request.acceptanceCriteria, 'Pendientes.')}

## Archivos afectados / probables

${bullets(request.likelyFiles, 'No definidos.')}

## Plan técnico

${bullets(request.technicalPlan, 'Pendiente.')}

## Pasos

${numbered(request.steps)}

## Salida esperada

${request.expectedOutput.trim()}

## Validación

${bullets(request.validation, 'Pendiente.')}

## Reversión

${request.rollback.trim()}

## Dependencias

${bullets(request.dependencies, 'Ninguna.')}

## Notas

${request.notes?.trim() || '—'}

## Issues vinculados

* Ninguno.

## Historial de eventos V2

\`\`\`jsonl
${eventJson}
\`\`\`
`;
  }

  function createTask(request, apply) {
    const effective = {
      ...request,
      authorName: request.authorName ?? process.env.IA_WORKFLOW_AUTHOR_NAME ?? null,
      authorEmail: request.authorEmail ?? process.env.IA_WORKFLOW_AUTHOR_EMAIL ?? null,
      authorInitials: request.authorInitials ?? process.env.IA_WORKFLOW_AUTHOR_INITIALS ?? null,
    };
    validateTaskRequest(effective);
    const resolvedInitials = normalizeInitials(effective.authorInitials || initials(effective.authorName || 'IA'));
    const area = requireChoice(effective.area.toUpperCase(), ['FE', 'BE', 'HF', 'DB', 'INF', 'DOC', 'MCP', 'ARCH', 'QA', 'CAP'], 'area');
    const id = store.nextTaskId(resolvedInitials, area);
    const relative = `04_tasks/tasks/${id}.md`;
    const ts = timestamp();
    const content = buildTaskDocument(id, effective, ts);
    const current = insertTaskRow(store.read('04_tasks/current.md'), 'Borradores', id, area, normalizeRisk(effective.risk));
    return store.commit('create_task', apply, { [relative]: content, '04_tasks/current.md': current }, id);
  }

  // ── assign_task_to_phase ─────────────────────────────────────────────────
  function assignTaskToPhase(id, phase, component, apply) {
    store.resolveTask(id, false);
    const plan = store.read('03_plan.md');
    const alreadyAssigned = new RegExp(`^\\|[^\\r\\n]*\\|\\s*[✅⏳]\\s+${id.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')}\\s*\\|`, 'm').test(plan);
    if (alreadyAssigned) throw new ToolError(`${id} ya está asignada a una fase activa.`);
    const section = phaseSection(plan, phase);
    if (section.text.includes('✅ Completada')) throw new ToolError('No se puede asignar una tarea a una fase completada.');
    const updatedSection = insertPlanRow(section.text, component, `⏳ ${id}`);
    const updated = plan.slice(0, section.start) + updatedSection + plan.slice(section.end);
    return store.commit('assign_task_to_phase', apply, { '03_plan.md': updated }, id);
  }

  // ── update_independent_plan ──────────────────────────────────────────────
  function updateIndependentPlan(planFile, planStatus, taskUpdates, phaseUpdates, apply) {
    const relative = independentPlanRelativePath(planFile);
    let updated = store.read(relative);
    if ((!planStatus || planStatus.trim() === '') && (!taskUpdates || taskUpdates.length === 0) && (!phaseUpdates || phaseUpdates.length === 0)) {
      throw new ToolError('Indique planStatus, taskUpdates o phaseUpdates.');
    }
    if (planStatus && planStatus.trim() !== '') updated = updateIndependentPlanStatus(updated, planStatus);
    for (const update of taskUpdates ?? []) updated = updateIndependentPlanTask(updated, update);
    for (const update of phaseUpdates ?? []) updated = updateIndependentPlanPhase(updated, update);
    return store.commit('update_independent_plan', apply, { [relative]: updated }, null);
  }

  // ── approve_task ──────────────────────────────────────────────────────────
  function approveTask(id, approver, apply) {
    const task = store.resolveTask(id, false);
    let text = store.read(task.relativePath);
    ensureStatus(text, 'Borrador');
    if (field(text, 'Riesgo').toLowerCase() === 'alto' && (!approver || approver.trim() === '')) {
      throw new ToolError('Las tareas de riesgo alto requieren approver explícito.');
    }
    text = setField(text, 'Estado', 'Lista');
    text = setField(text, 'Aprobación', 'aprobada');
    text = setField(text, 'Aprobado por', approver || 'aprobación explícita');
    text = setField(text, 'approvedAt', timestamp());
    text = appendEvent(text, id, 'approved', approver || 'aprobación explícita');
    const current = moveTaskRow(store.read('04_tasks/current.md'), id, 'Lista', field(text, 'Área'), field(text, 'Riesgo'));
    return store.commit('approve_task', apply, { [task.relativePath]: text, '04_tasks/current.md': current }, id);
  }

  // ── work_task ─────────────────────────────────────────────────────────────
  function workTask(id, transition, reason, apply) {
    const task = store.resolveTask(id, false);
    let text = store.read(task.relativePath);
    const currentStatus = status(text);
    if (transition !== null && transition !== undefined && transition !== 'blocked' && transition !== 'resumed') {
      throw new ToolError('transition debe ser blocked o resumed.');
    }
    if (transition && (!reason || reason.trim() === '')) throw new ToolError('reason es obligatorio para blocked o resumed.');

    if (!transition) {
      ensureStatus(text, 'Lista');
      text = setField(text, 'Estado', 'En progreso');
      text = setField(text, 'startedAt', timestamp());
      text = appendEvent(text, id, 'started', 'Inicio de implementación autorizado.');
      const current = moveTaskRow(store.read('04_tasks/current.md'), id, 'En progreso', field(text, 'Área'), field(text, 'Riesgo'));
      return store.commit('work_task', apply, { [task.relativePath]: text, '04_tasks/current.md': current }, id);
    }

    if (transition === 'blocked') {
      if (currentStatus.toLowerCase() !== 'en progreso') throw new ToolError('Solo una tarea En progreso puede bloquearse.');
      text = setField(text, 'Estado', 'Bloqueada');
      text = appendEvent(text, id, 'blocked', reason);
      const current = moveTaskRow(store.read('04_tasks/current.md'), id, 'Bloqueadas', field(text, 'Área'), field(text, 'Riesgo'));
      const blocked = addIndexLine(store.readOptional('04_tasks/blocked.md'), id, reason);
      return store.commit('work_task', apply, { [task.relativePath]: text, '04_tasks/current.md': current, '04_tasks/blocked.md': blocked }, id);
    }

    if (currentStatus.toLowerCase() !== 'bloqueada') throw new ToolError('Solo una tarea Bloqueada puede reanudarse.');
    text = setField(text, 'Estado', 'En progreso');
    text = appendEvent(text, id, 'resumed', reason);
    const resumedCurrent = moveTaskRow(store.read('04_tasks/current.md'), id, 'En progreso', field(text, 'Área'), field(text, 'Riesgo'));
    const resumedBlocked = removeIndexLine(store.readOptional('04_tasks/blocked.md'), id);
    return store.commit('work_task', apply, { [task.relativePath]: text, '04_tasks/current.md': resumedCurrent, '04_tasks/blocked.md': resumedBlocked }, id);
  }

  // ── return_task_to_draft ─────────────────────────────────────────────────
  function returnTaskToDraft(id, reason, apply) {
    const task = store.resolveTask(id, false);
    let text = store.read(task.relativePath);
    if (status(text).toLowerCase() === 'borrador') throw new ToolError('La tarea ya está en Borrador.');
    text = setField(text, 'Estado', 'Borrador');
    text = setField(text, 'Aprobación', 'pendiente');
    text = setField(text, 'Aprobado por', '—');
    text = setField(text, 'approvedAt', '—');
    text = setField(text, 'startedAt', '—');
    text = appendEvent(text, id, 'returned_to_draft', require_(reason, 'reason'));
    const current = moveTaskRow(store.read('04_tasks/current.md'), id, 'Borradores', field(text, 'Área'), field(text, 'Riesgo'));
    return store.commit(
      'return_task_to_draft',
      apply,
      { [task.relativePath]: text, '04_tasks/current.md': current, '04_tasks/blocked.md': removeIndexLine(store.readOptional('04_tasks/blocked.md'), id) },
      id,
    );
  }

  // ── finish_task ───────────────────────────────────────────────────────────
  function finishTask(id, summaryText, apply) {
    const task = store.resolveTask(id, false);
    let text = store.read(task.relativePath);
    ensureStatus(text, 'En progreso');
    text = setField(text, 'Estado', 'Completada');
    text = setField(text, 'finishedAt', timestamp());
    text = appendEvent(text, id, 'completed', summaryText || 'Tarea completada.');
    const month = costaRicaYearMonth();
    const archive = `04_tasks/done/${month}.md`;
    let archiveText = store.readOptional(archive);
    if (!archiveText.includes(`# ${id} `)) {
      archiveText = archiveText.trim() === '' ? text : archiveText.replace(/\s+$/, '') + '\n\n---\n\n' + text;
    }
    const { plan, phaseCompleted, phaseName } = markPlanTaskComplete(store.read('03_plan.md'), id);
    const changes = {
      [archive]: archiveText,
      '04_tasks/current.md': removeTaskRow(store.read('04_tasks/current.md'), id),
      '04_tasks/blocked.md': removeIndexLine(store.readOptional('04_tasks/blocked.md'), id),
      '03_plan.md': plan,
      '05_progress/current.md': addProgress(store.readOptional('05_progress/current.md'), `${id}: ${summaryText || 'tarea completada'}`),
    };
    if (phaseCompleted && phaseName) {
      changes['02_architecture.md'] = addReviewFlag(store.readOptional('02_architecture.md'), phaseName, 'architecture');
      changes['08_retrospective.md'] = addReviewFlag(store.readOptional('08_retrospective.md'), phaseName, 'retrospective');
    }
    return store.commit('finish_task', apply, changes, id, [task.relativePath]);
  }

  // ── restore_task ──────────────────────────────────────────────────────────
  function restoreTask(id, reason, apply) {
    const archived = store.resolveTask(id, true);
    if (!archived.archived) throw new ToolError('restore_task requiere una tarea archivada.');
    let text = extractArchivedTask(store.read(archived.relativePath), id);
    text = setField(text, 'Estado', 'Borrador');
    text = setField(text, 'Aprobación', 'pendiente');
    text = setField(text, 'Aprobado por', '—');
    text = setField(text, 'approvedAt', '—');
    text = setField(text, 'startedAt', '—');
    text = setField(text, 'finishedAt', '—');
    text = appendEvent(text, id, 'restored', require_(reason, 'reason'));
    const target = `04_tasks/tasks/${id}.md`;
    return store.commit(
      'restore_task',
      apply,
      {
        [target]: text,
        '04_tasks/current.md': insertTaskRow(store.read('04_tasks/current.md'), 'Borradores', id, field(text, 'Área'), field(text, 'Riesgo')),
        '04_tasks/blocked.md': removeIndexLine(store.readOptional('04_tasks/blocked.md'), id),
      },
      id,
    );
  }

  // ── reopen_task ───────────────────────────────────────────────────────────
  function reopenTask(id, issueId, reason, apply) {
    store.resolveOpenIssue(issueId);
    const archived = store.resolveTask(id, true);
    if (!archived.archived) throw new ToolError('reopen_task requiere una tarea archivada.');
    const result = restoreTask(id, reason, apply);
    if (!apply) return { ...result, message: 'Preview de reapertura: restaura la tarea y enlaza el issue abierto.' };
    return linkIssueToTask(id, issueId, reason, true);
  }

  // ── duplicate_task ────────────────────────────────────────────────────────
  function duplicateTask(id, duplicateOf, reason, apply) {
    const task = store.resolveTask(id, false);
    if (id.toLowerCase() === duplicateOf.toLowerCase()) throw new ToolError('duplicateOf debe ser otra tarea.');
    store.resolveTask(duplicateOf, true);
    let text = store.read(task.relativePath);
    text = setField(text, 'Estado', 'Duplicada');
    text = appendEvent(text, id, 'duplicated', `Duplicada de ${duplicateOf}: ${require_(reason, 'reason')}`);
    const archive = `04_tasks/done/${costaRicaYearMonth()}.md`;
    let archiveText = store.readOptional(archive);
    archiveText = archiveText.trim() === '' ? text : archiveText.replace(/\s+$/, '') + '\n\n---\n\n' + text;
    return store.commit(
      'duplicate_task',
      apply,
      { [archive]: archiveText, '04_tasks/current.md': removeTaskRow(store.read('04_tasks/current.md'), id), '04_tasks/blocked.md': removeIndexLine(store.readOptional('04_tasks/blocked.md'), id) },
      id,
      [task.relativePath],
    );
  }

  // ── delete_task ───────────────────────────────────────────────────────────
  function deleteTask(id, reason, confirm, apply) {
    if (confirm !== true) throw new ToolError('delete_task requiere confirm=true.');
    const task = store.resolveTask(id, false);
    let text = setField(store.read(task.relativePath), 'Estado', 'Eliminada');
    text = appendEvent(text, id, 'deleted', require_(reason, 'reason'));
    const archive = `04_tasks/done/${costaRicaYearMonth()}.md`;
    let archiveText = store.readOptional(archive);
    archiveText = archiveText.trim() === '' ? text : archiveText.replace(/\s+$/, '') + '\n\n---\n\n' + text;
    return store.commit(
      'delete_task',
      apply,
      { [archive]: archiveText, '04_tasks/current.md': removeTaskRow(store.read('04_tasks/current.md'), id), '04_tasks/blocked.md': removeIndexLine(store.readOptional('04_tasks/blocked.md'), id) },
      id,
      [task.relativePath],
    );
  }

  // ── ia_create_issue ───────────────────────────────────────────────────────
  function createIssue(title, severity, component, symptom, apply) {
    require_(title, 'title');
    require_(symptom, 'symptom');
    const normalizedSeverity = requireChoice(severity.toLowerCase(), ['low', 'medium', 'high', 'critical'], 'severity');
    const id = `ISSUE-${store.nextNumber('ISSUE-', '07_issues', 3)}`;
    const relative = `07_issues/open/${id}-${slug(title)}.md`;
    const body = `# ${id} — ${title.trim()}\n\n**Estado:** Abierto\n**Severidad:** ${normalizedSeverity}\n**Componente:** ${component.trim()}\n**createdAt:** ${timestamp()}\n\n## Síntoma\n\n${symptom.trim()}\n\n## Tareas vinculadas\n\n* Ninguna.\n`;
    const current = addIndexLine(store.readOptional('07_issues/current.md'), id, `${normalizedSeverity} | ${component.trim()} | ${title.trim()}`);
    return store.commit('ia_create_issue', apply, { [relative]: body, '07_issues/current.md': current }, id);
  }

  // ── close_issue ───────────────────────────────────────────────────────────
  function closeIssue(id, resolution, component, apply) {
    const issue = store.resolveOpenIssue(id);
    let text = setField(store.read(issue.relativePath), 'Estado', 'Cerrado');
    text += `\n## Resolución\n\n${require_(resolution, 'resolution')}\n`;
    const archive = `07_issues/archive/${costaRicaYearMonth()}.md`;
    let archived = store.readOptional(archive);
    archived = archived.trim() === '' ? text : archived.replace(/\s+$/, '') + '\n\n---\n\n' + text;
    const changes = {
      [archive]: archived,
      '07_issues/current.md': removeIndexLine(store.readOptional('07_issues/current.md'), id),
      '05_progress/current.md': addProgress(store.readOptional('05_progress/current.md'), `${id} cerrado: ${resolution.trim()}`),
    };
    if (component && component.trim() !== '') {
      const path = `05_progress/by-component/${slug(component)}.md`;
      changes[path] = addProgress(store.readOptional(path), `${id} cerrado: ${resolution.trim()}`);
    }
    return store.commit('close_issue', apply, changes, id, [issue.relativePath]);
  }

  // ── ia_link_issue_to_task ─────────────────────────────────────────────────
  function linkIssueToTask(taskIdValue, issueId, reason, apply) {
    const task = store.resolveTask(taskIdValue, false);
    const issue = store.resolveOpenIssue(issueId);
    const taskText = addBulletUnderHeading(store.read(task.relativePath), 'Issues vinculados', issueId, reason);
    const issueText = addBulletUnderHeading(store.read(issue.relativePath), 'Tareas vinculadas', taskIdValue, reason);
    return store.commit('ia_link_issue_to_task', apply, { [task.relativePath]: taskText, [issue.relativePath]: issueText }, taskIdValue);
  }

  // ── ia_add_progress_entry ────────────────────────────────────────────────
  function addProgressEntry(text, component, authorInitials, apply) {
    const entry = `${timestamp()} ${!authorInitials || authorInitials.trim() === '' ? 'IA' : normalizeInitials(authorInitials)}: ${require_(text, 'text')}`;
    const changes = { '05_progress/current.md': addProgress(store.readOptional('05_progress/current.md'), entry) };
    if (component && component.trim() !== '') {
      const path = `05_progress/by-component/${slug(component)}.md`;
      changes[path] = addProgress(store.readOptional(path), entry);
    }
    return store.commit('ia_add_progress_entry', apply, changes, null);
  }

  // ── archive_progress ──────────────────────────────────────────────────────
  function archiveProgress(keepDays, archiveAllClosed, apply) {
    const current = store.readOptional('05_progress/current.md');
    const shouldArchive = archiveAllClosed === true || (keepDays !== undefined && keepDays !== null && keepDays <= 0);
    if (!shouldArchive) return { preview: true, applied: false, message: 'No hay entradas elegibles para archivar.', changes: [] };
    const sectionMatch = /^## (?:Completado en sesiones recientes|Últimas tareas completadas)\s*\n([\s\S]*?)(?=^## |$(?![\s\S]))/m.exec(current);
    if (!sectionMatch || sectionMatch[1].trim() === '' || sectionMatch[1].includes('Sin entradas registradas.')) {
      return { preview: !apply, applied: apply, message: 'No hay entradas cerradas para archivar.', changes: [] };
    }
    const archive = `05_progress/archive/${costaRicaYearMonth()}.md`;
    const archived = store.readOptional(archive).replace(/\s+$/, '') + '\n\n' + sectionMatch[0].trim();
    const cleaned = (current.slice(0, sectionMatch.index) + current.slice(sectionMatch.index + sectionMatch[0].length)).replace(/\s+$/, '') + '\n';
    return store.commit('archive_progress', apply, { [archive]: archived, '05_progress/current.md': cleaned }, null);
  }

  // ── ia_create_decision ────────────────────────────────────────────────────
  function createDecision(title, domain, context, decision, reason, statusValue, alternatives, consequences, replaces, apply) {
    require_(title, 'title');
    require_(domain, 'domain');
    require_(context, 'context');
    require_(decision, 'decision');
    require_(reason, 'reason');
    const normalizedStatus = requireChoice(statusValue || 'propuesta', ['propuesta', 'aceptada', 'reemplazada'], 'status');
    const id = `ADR-${store.nextNumber('ADR-', '06_decisions', 2)}`;
    const relative = `06_decisions/${id}-${slug(title)}.md`;
    const dateStr = costaRicaDate();
    let body = `# ${id} — ${title.trim()}\n\n**Estado:** ${normalizedStatus}\n**Dominio:** ${domain.trim()}\n**Fecha:** ${dateStr}\n\n## Contexto\n\n${context.trim()}\n\n## Decisión\n\n${decision.trim()}\n\n## Razón\n\n${reason.trim()}\n\n## Alternativas\n\n${bullets(alternatives, 'Ninguna documentada.')}\n\n## Consecuencias\n\n${bullets(consequences, 'Pendientes de validar.')}\n`;
    if (replaces && replaces.trim() !== '') body += `\n## Reemplaza\n\n* ${replaces.trim()}\n`;
    const index = store.readOptional('06_decisions.md');
    const row = `| ${id} | ${title.trim()} | ${normalizedStatus} | ${domain.trim()} | ${dateStr} | [${path.basename(relative)}](${relative}) |`;
    const updatedIndex = index.replace(/\s+$/, '') + '\n' + row + '\n';
    return store.commit('ia_create_decision', apply, { [relative]: body, '06_decisions.md': updatedIndex }, id);
  }

  // ── resolve_phase_review ──────────────────────────────────────────────────
  function resolvePhaseReview(phase, review, outcome, evidence, references, apply) {
    const normalizedReview = requireChoice(review, ['architecture', 'retrospective'], 'review');
    const normalizedOutcome = requireChoice(outcome, ['updated', 'recorded', 'not_applicable'], 'outcome');
    if (normalizedReview === 'architecture' && normalizedOutcome === 'recorded') throw new ToolError('architecture acepta updated o not_applicable.');
    if (normalizedReview === 'retrospective' && normalizedOutcome === 'updated') throw new ToolError('retrospective acepta recorded o not_applicable.');
    if (normalizedOutcome === 'updated' && (!references || references.length === 0)) throw new ToolError('updated requiere references.');
    const relative = normalizedReview === 'architecture' ? '02_architecture.md' : '08_retrospective.md';
    const text = store.readOptional(relative);
    if (!text.includes(phase)) throw new ToolError(`No existe un flag de ${normalizedReview} para ${phase}.`);
    const lines = text.split('\n').filter((line) => !line.includes(phase));
    const section = normalizedReview === 'architecture' ? '## Revisiones arquitectónicas de fases' : '## Revisiones de retrospectiva de fases';
    const record = `* ${phase}: ${normalizedOutcome}. Evidencia: ${require_(evidence, 'evidence')}` + (references && references.length > 0 ? ` Referencias: ${references.join(', ')}.` : '');
    const index = lines.findIndex((line) => line.startsWith(section));
    if (index < 0) {
      lines.push('', section, '', record);
    } else {
      lines.splice(index + 1, 0, record);
    }
    return store.commit('resolve_phase_review', apply, { [relative]: lines.join('\n').replace(/\s+$/, '') + '\n' }, phase);
  }

  // ── migrate_tasks_to_v2 ───────────────────────────────────────────────────
  function migrateTasksToV2(reason, apply) {
    require_(reason, 'reason');
    const candidates = store.enumerateMarkdown('04_tasks/tasks').filter((full) => !readFileSync(full, 'utf8').includes('**schemaVersion:** 2'));
    if (candidates.length === 0) return { preview: true, applied: false, message: 'No hay tareas V1 para migrar.', changes: [] };

    const planned = {};
    for (const full of candidates) {
      planned[store.relative(full)] = migrateTaskContentToV2(readFileSync(full, 'utf8'));
    }
    const changes = Object.keys(planned).map((rel) => ({ path: rel, action: 'update' }));
    if (!apply) {
      return { preview: true, applied: false, message: 'Preview de migración V1 a V2; no se modificó ningún archivo.', changes, data: { candidates: Object.keys(planned) } };
    }
    return store.commit('migrate_tasks_to_v2', apply, planned, null);
  }

  function migrateTaskContentToV2(text) {
    if (text.includes('**schemaVersion:** 2') && text.includes('## Historial de eventos V2')) return text;
    const headerMatch = /^#\s+(TASK-[A-Z0-9]{1,3}-[A-Z]+-\d+)\s*—\s*([^\n\r]+)([\s\S]*?\n---\n)/m.exec(text);
    if (!headerMatch) {
      return text.replace(/^(#\s+TASK-[^\n\r]+)/m, '$1\n\n**schemaVersion:** 2');
    }
    const id = headerMatch[1].trim();
    const title = headerMatch[2].trim();
    const metaBlock = headerMatch[3];
    const body = text.slice(headerMatch.index + headerMatch[0].length);

    const orDash = (v, fallback) => (!v || v.trim() === '' || v === '-' ? fallback : v);
    const estado = orDash(field(metaBlock, 'Estado'), 'Borrador');
    const autor = orDash(field(metaBlock, 'Autor'), '—');
    const rama = orDash(field(metaBlock, 'Rama'), '—');
    const inicio = orDash(field(metaBlock, 'Inicio'), '—');
    const cierre = orDash(field(metaBlock, 'Cierre'), '—');
    const area = orDash(field(metaBlock, 'Área'), 'GEN');
    const prioridad = orDash(field(metaBlock, 'Prioridad'), 'media');
    const riesgo = orDash(field(metaBlock, 'Riesgo'), 'medio');
    const aprobacionRaw = field(metaBlock, 'Aprobación');
    const aprobacion = !aprobacionRaw || aprobacionRaw === '-' ? (estado === 'Borrador' ? 'pendiente' : 'Aprobada') : aprobacionRaw;

    let newBody = body;
    if (!newBody.includes('## Historial de eventos V2')) {
      const eventJson = JSON.stringify({
        schemaVersion: 2,
        eventId: randomUUID(),
        taskId: id,
        event: 'migrated_to_v2',
        occurredAt: timestamp(),
        actor: 'MCP iaWorkflow',
        reason: 'Migración de tarea V1 a contrato V2.',
      });
      newBody = newBody.replace(/\s+$/, '') + `\n\n## Historial de eventos V2\n\n\`\`\`jsonl\n${eventJson}\n\`\`\`\n`;
    }

    const newHeader = `# ${id} — ${title}

**schemaVersion:** 2
**Estado:** ${estado}
**DevOps:** —
**CRM:** —
**Autor:** ${autor}
**Aprobado por:** —
**Rama:** ${rama}
**createdAt:** —
**approvedAt:** —
**startedAt:** ${inicio}
**finishedAt:** ${cierre}
**Área:** ${area.toUpperCase()}
**Prioridad:** ${prioridad.toLowerCase()}
**Riesgo:** ${normalizeRisk(riesgo)}
**Aprobación:** ${aprobacion}
**parentId:** —
**workItemType:** Task

---
`;
    return newHeader + newBody;
  }

  return {
    validate,
    getContext,
    inspect,
    readResource,
    createTask,
    assignTaskToPhase,
    updateIndependentPlan,
    approveTask,
    workTask,
    returnTaskToDraft,
    finishTask,
    restoreTask,
    reopenTask,
    duplicateTask,
    deleteTask,
    createIssue,
    closeIssue,
    linkIssueToTask,
    addProgressEntry,
    archiveProgress,
    createDecision,
    resolvePhaseReview,
    migrateTasksToV2,
  };
}

function costaRicaDateParts() {
  const parts = new Intl.DateTimeFormat('en-US', {
    timeZone: 'America/Costa_Rica',
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
  }).formatToParts(new Date());
  const get = (type) => parts.find((p) => p.type === type).value;
  return { year: get('year'), month: get('month'), day: get('day') };
}

function costaRicaYearMonth() {
  const { year, month } = costaRicaDateParts();
  return `${year}-${month}`;
}

function costaRicaDate() {
  const { year, month, day } = costaRicaDateParts();
  return `${year}-${month}-${day}`;
}
