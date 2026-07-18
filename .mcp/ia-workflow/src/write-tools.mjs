import fs from "node:fs/promises";
import path from "node:path";
import { capitalize, clampInteger, escapeRegExp, initialsFromName, looksMostlyEnglish, normalizePriority, requireEnum, requireSpanishText, requireString, requireStringArray, rpcError, sanitizeIssueId, sanitizeTaskId, slugify } from "./common.mjs";
import { componentFromArea, ensureTrailingNewline, insertAfterHeading, insertAfterTableHeader, insertTableRow, markPlanTaskDone, removeTaskRows, simpleDiff, summarizeMarkdown, updateLastUpdatedLine } from "./markdown.mjs";
import { currentCrTimestamp, todayCrDate, todayCrMonth } from "./time.mjs";
import { scanTextForSecrets } from "./secrets.mjs";

export function createWriteTools(ctx) {
  const { iaRoot, listMarkdownFiles, normalizeRelativePath, optionalText, readText, safeIaPath, validateIa } = ctx;

async function previewOperation(input) {
  const operation = requireString(input.operation, "operation");
  const operationArgs = input.arguments ?? {};
  return runWriteOperation(operation, { ...operationArgs, apply: false });
}

async function runWriteOperation(operation, input) {
  const apply = input.apply === true;
  const changes = await buildOperationChanges(operation, { ...input, apply });
  const preview = {
    operation,
    apply,
    changes: changes.map((change) => describeChange(change)),
  };

  const preflight = await validatePlannedChanges(changes);
  if (!preflight.valid) {
    return {
      ...preview,
      applied: false,
      validation: preflight,
    };
  }

  if (!apply) {
    return {
      ...preview,
      applied: false,
      guidance: "Preview generado. Para aplicar exactamente esta operacion, llama la misma tool con apply=true.",
      validation: preflight,
    };
  }

  await applyChanges(changes);
  return {
    ...preview,
    applied: true,
    validation: await validateIa(),
  };
}

async function buildOperationChanges(operation, input) {
  switch (operation) {
    case "create_task":
      return buildCreateTaskChanges(input);
    case "approve_task":
      return buildApproveTaskChanges(input);
    case "finish_task":
      return buildFinishTaskChanges(input);
    case "close_task":
      return buildCloseTaskChanges(input);
    case "add_progress_entry":
      return buildAddProgressEntryChanges(input);
    case "create_issue":
      return buildCreateIssueChanges(input);
    case "close_issue":
      return buildCloseIssueChanges(input);
    case "create_decision":
      return buildCreateDecisionChanges(input);
    case "archive_progress":
      return buildArchiveProgressChanges(input);
    default:
      throw rpcError(-32602, `Operacion no soportada: ${operation}`);
  }
}

async function buildCreateTaskChanges(input) {
  const title = requireSpanishText(input.title, "title");
  const area = requireEnum(input.area, "area", ["FE", "BE", "DB", "INF", "DOC", "MCP", "QA"]);
  const priority = normalizePriority(input.priority ?? "media");
  const risk = normalizeRisk(input.risk ?? "medio");
  const approval = normalizeApproval(input.approval ?? "pendiente");
  const status = normalizeTaskStatus(input.status ?? "Borrador");
  const authorName = input.authorName ? String(input.authorName).trim() : "Ezequiel Baltodano Cubillo";
  const authorEmail = input.authorEmail ? String(input.authorEmail).trim() : "pendiente";
  const branch = input.branch ? String(input.branch).trim() : "dev";
  const context = requireSpanishText(input.context, "context");
  const steps = requireStringArray(input.steps, "steps");
  const expectedOutput = requireSpanishText(input.expectedOutput, "expectedOutput");
  const objective = input.objective ? requireSpanishText(input.objective, "objective") : title;
  const allowedScope = readSpanishArrayOrDefault(input.allowedScope, "allowedScope", steps);
  const outOfScope = readSpanishArrayOrDefault(input.outOfScope, "outOfScope", ["No definido."]);
  const acceptanceCriteria = readSpanishArrayOrDefault(input.acceptanceCriteria, "acceptanceCriteria", [expectedOutput]);
  const likelyFiles = readPlainArrayOrDefault(input.likelyFiles, ["pendiente de confirmar"]);
  const technicalPlan = readSpanishArrayOrDefault(input.technicalPlan, "technicalPlan", steps);
  const validation = readSpanishArrayOrDefault(input.validation, "validation", ["Validacion pendiente de definir."]);
  const rollback = input.rollback ? requireSpanishText(input.rollback, "rollback") : "Revertir los cambios de la tarea en control de versiones.";
  const dependencies = readPlainArrayOrDefault(input.dependencies, ["ninguna"]);
  const notes = input.notes ? String(input.notes).trim() : "Sin notas adicionales.";
  const initials = initialsFromName(authorName);
  const id = await nextTaskId(initials, area);
  const taskPath = `04_tasks/tasks/${id}.md`;
  const startedAt = currentCrTimestamp();

  const taskText = `# ${id} — ${title}

**Estado:** ${status}
**Autor:** ${authorName} \`<${authorEmail}>\`
**Rama:** \`${branch}\`
**Fecha inicio:** ${startedAt}
**Fecha cierre:** —
**Área:** ${area}
**Prioridad:** ${priority}
**Riesgo:** ${risk}
**Aprobación:** ${approval}

---

## Título

${title}

## Contexto

${context}

## Objetivo

${objective}

## Alcance permitido

${allowedScope.map((item) => `* ${item}`).join("\n")}

## Fuera de alcance

${outOfScope.map((item) => `* ${item}`).join("\n")}

## Criterios de aceptación

${acceptanceCriteria.map((item) => `* [ ] ${item}`).join("\n")}

## Riesgos

${risk === "alto" ? "Riesgo alto: requiere aprobación explícita antes de implementar." : `Riesgo ${risk}.`}

## Archivos afectados / probables

${likelyFiles.map((item) => `* \`${item}\``).join("\n")}

## Plan técnico

${technicalPlan.map((step, index) => `${index + 1}. ${step}`).join("\n")}

## Pasos

${steps.map((step, index) => `${index + 1}. ${step}`).join("\n")}

## Salida esperada

${expectedOutput}

## Validación

${validation.map((item) => `* [ ] ${item}`).join("\n")}

## Rollback

${rollback}

## Dependencias

${dependencies.map((item) => `* ${item}`).join("\n")}

## Checklist

* [ ] Alcance revisado
* [ ] Riesgo revisado
* [ ] Aprobación registrada si aplica
* [ ] Implementación completa
* [ ] Validación completa
* [ ] Progreso/documentación actualizado

## Notas / contexto adicional

${notes}

## Issues vinculados

* ninguno
`;

  const currentPath = "04_tasks/current.md";
  const currentText = await optionalText(currentPath);
  const row = status === "Borrador"
    ? `| ${id} | ${area} | ${title} | ${priority} | ${risk} |`
    : `| ${id} | ${area} | ${title} | ${priority} |`;
  const updatedCurrent = insertTableRow(
    updateLastUpdatedLine(currentText, `${todayCrDate()} CR (${id} creada)`),
    currentHeadingForStatus(status),
    row
  );

  return [
    { action: "create", path: taskPath, after: taskText },
    { action: "update", path: currentPath, before: currentText, after: updatedCurrent },
  ];
}

async function buildApproveTaskChanges(input) {
  const id = sanitizeTaskId(requireString(input.id, "id"));
  const approver = input.approver ? String(input.approver).trim() : "usuario";
  const taskPath = `04_tasks/tasks/${id}.md`;
  const taskText = await optionalText(taskPath);
  if (!taskText) {
    throw rpcError(-32602, `No existe la tarea activa ${id}`);
  }

  const contract = inspectTaskContract(taskText);
  if (contract.status !== "Borrador" && contract.status !== "Lista") {
    throw rpcError(-32602, `Solo se pueden aprobar tareas en Borrador o Lista. Estado actual: ${contract.status}`);
  }

  assertTaskContractComplete(contract);

  const approvedTask = appendTaskNote(
    replaceTaskField(replaceTaskField(taskText, "Estado", "Lista"), "Aprobación", "aprobada"),
    `Aprobada por ${approver} el ${currentCrTimestamp()}.`
  );

  const currentPath = "04_tasks/current.md";
  const currentText = await optionalText(currentPath);
  const row = `| ${id} | ${contract.area} | ${contract.title} | ${contract.priority} |`;
  const updatedCurrent = insertTableRow(
    updateLastUpdatedLine(removeTaskRows(currentText, id), `${todayCrDate()} CR (${id} aprobada)`),
    "## Próximas (Lista / ready to start — en orden de implementación)",
    row
  );

  return [
    { action: "update", path: taskPath, before: taskText, after: approvedTask },
    { action: "update", path: currentPath, before: currentText, after: updatedCurrent },
  ];
}

async function buildFinishTaskChanges(input) {
  const outcome = requireEnum(input.outcome ?? "review", "outcome", ["review", "done"]);
  if (outcome === "done") {
    return buildCloseTaskChanges(input);
  }

  const id = sanitizeTaskId(requireString(input.id, "id"));
  const summary = requireSpanishText(input.summary, "summary");
  const area = requireEnum(input.area, "area", ["FE", "BE", "DB", "INF", "DOC", "MCP", "QA"]);
  const taskPath = `04_tasks/tasks/${id}.md`;
  const taskText = await optionalText(taskPath);
  if (!taskText) {
    throw rpcError(-32602, `No existe la tarea activa ${id}`);
  }

  const contract = inspectTaskContract(taskText);
  const reviewTask = appendTaskNote(replaceTaskField(taskText, "Estado", "En revisión"), `Pendiente de revisión: ${summary}`);
  const currentPath = "04_tasks/current.md";
  const currentText = await optionalText(currentPath);
  const row = `| ${id} | ${area} | ${contract.title} | ${contract.priority} |`;
  const updatedCurrent = insertTableRow(
    updateLastUpdatedLine(removeTaskRows(currentText, id), `${todayCrDate()} CR (${id} en revisión)`),
    "## En revisión",
    row
  );

  return [
    { action: "update", path: taskPath, before: taskText, after: reviewTask },
    { action: "update", path: currentPath, before: currentText, after: updatedCurrent },
  ];
}

async function workTask(input) {
  const id = sanitizeTaskId(requireString(input.id, "id"));
  const mode = requireEnum(input.mode ?? "summary", "mode", ["summary", "full"]);
  const maxChars = clampInteger(input.maxChars ?? 12000, 0, 50000);
  const taskPath = `04_tasks/tasks/${id}.md`;
  const taskText = await optionalText(taskPath);
  if (!taskText) {
    throw rpcError(-32602, `No existe la tarea activa ${id}`);
  }

  const contract = inspectTaskContract(taskText);
  assertTaskCanBeWorked(contract);

  const contextPaths = [
    "00_context.md",
    "02_architecture.md",
    "06_decisions.md",
    "07_issues/current.md",
    taskPath,
  ];
  const files = [];
  for (const relativePath of contextPaths) {
    const text = await optionalText(relativePath);
    if (!text) {
      continue;
    }

    files.push({
      path: relativePath,
      chars: text.length,
      ...(mode === "full"
        ? { text: maxChars > 0 && text.length > maxChars ? `${text.slice(0, maxChars)}\n\n[truncado]` : text }
        : { summary: summarizeMarkdown(text) }),
    });
  }

  return {
    id,
    path: taskPath,
    allowed: true,
    readonly: true,
    note: "work_task valida gates y devuelve contexto. La edicion de codigo ocurre en el workspace del agente, no dentro del MCP.",
    status: contract.status,
    risk: contract.risk,
    approval: contract.approval,
    likelyFiles: contract.likelyFiles,
    files,
    nextSteps: [
      "Implementar solo dentro del alcance permitido de la TASK.",
      "Ejecutar las validaciones declaradas en la TASK.",
      "Usar finish_task con outcome=review o outcome=done al terminar.",
    ],
  };
}

async function buildCloseTaskChanges(input) {
  const id = sanitizeTaskId(requireString(input.id, "id"));
  const summary = requireSpanishText(input.summary, "summary");
  const area = requireEnum(input.area, "area", ["FE", "BE", "DB", "INF", "DOC", "MCP", "QA"]);
  const authorName = input.authorName ? String(input.authorName).trim() : "Ezequiel Baltodano Cubillo";
  const component = input.progressComponent ?? componentFromArea(area);
  const taskPath = `04_tasks/tasks/${id}.md`;
  const taskText = await optionalText(taskPath);
  if (!taskText) {
    throw rpcError(-32602, `No existe la tarea activa ${id}`);
  }

  const contract = inspectTaskContract(taskText);
  assertTaskCanBeFinished(contract);
  const filesChanged = readPlainArrayOrDefault(input.filesChanged, ["No documentados."]);
  const validation = readSpanishArrayOrDefault(input.validation, "validation", ["Validación declarada en la tarea completada."]);
  const pendingItems = readSpanishArrayOrDefault(input.pendingItems, "pendingItems", ["Ninguno."]);
  const risks = readSpanishArrayOrDefault(input.risks, "risks", ["Ninguno adicional."]);
  const rollbackNotes = input.rollbackNotes
    ? requireSpanishText(input.rollbackNotes, "rollbackNotes")
    : "Usar el rollback documentado en la tarea original.";

  const currentPath = "04_tasks/current.md";
  const currentText = await optionalText(currentPath);
  const updatedCurrent = updateLastUpdatedLine(
    removeTaskRows(currentText, id),
    `${todayCrDate()} CR (${id} completada)`
  );

  const donePath = `04_tasks/done/${todayCrMonth()}.md`;
  const doneText = await optionalText(donePath);
  const doneBase = doneText || "# Tareas completadas\n\n| Fecha cierre | ID | Título | Área | Autor |\n|---|---|---|---|---|\n";
  const doneRow = `| ${currentCrTimestamp()} | ${id} | ${summary} | ${area} | ${authorName} |`;
  const doneDetail = `## ${id} — Detalle de cierre

**Resumen:** ${summary}
**Área:** ${area}
**Autor:** ${authorName}
**Cierre:** ${currentCrTimestamp()}

### Archivos cambiados

${filesChanged.map((item) => `* \`${item}\``).join("\n")}

### Validación ejecutada

${validation.map((item) => `* ${item}`).join("\n")}

### Pendientes

${pendingItems.map((item) => `* ${item}`).join("\n")}

### Riesgos residuales

${risks.map((item) => `* ${item}`).join("\n")}

### Notas de rollback

${rollbackNotes}
`;
  const doneWithRow = insertAfterTableHeader(doneBase, doneRow);
  const updatedDone = `${doneWithRow.trimEnd()}\n\n${doneDetail}`;

  const progressPath = "05_progress/current.md";
  const progressText = await optionalText(progressPath);
  const progressEntry = `* **${todayCrDate()}** — ${id} cerrada: ${summary} — ${initialsFromName(authorName)}`;
  const updatedProgress = insertAfterHeading(
    updateLastUpdatedLine(progressText, `${todayCrDate()} CR (${id} completada)`),
    "## Completado en sesiones recientes",
    progressEntry
  );

  const changes = [
    { action: "update", path: currentPath, before: currentText, after: updatedCurrent },
    { action: doneText ? "update" : "create", path: donePath, before: doneText || undefined, after: updatedDone },
    { action: "update", path: progressPath, before: progressText, after: updatedProgress },
    { action: "delete", path: taskPath, before: taskText },
  ];

  if (component) {
    const componentPath = `05_progress/by-component/${component}.md`;
    const componentText = await optionalText(componentPath);
    const componentEntry = `* **${todayCrDate()}** — ${id}: ${summary} — ${initialsFromName(authorName)}`;
    changes.push({
      action: "update",
      path: componentPath,
      before: componentText,
      after: insertAfterHeading(
        updateLastUpdatedLine(componentText, `${todayCrDate()} CR (${id} completada)`),
        "## Completado",
        componentEntry
      ),
    });
  }

  // Actualizar 03_plan.md si el task ID aparece en el plan de fases
  const planPath = "03_plan.md";
  const planText = await optionalText(planPath);
  if (planText && planText.includes(id)) {
    const updatedPlan = markPlanTaskDone(
      updateLastUpdatedLine(planText, `${todayCrDate()} CR (${id} completada)`),
      id
    );
    if (updatedPlan !== planText) {
      changes.push({ action: "update", path: planPath, before: planText, after: updatedPlan });
    }
  }

  return changes;
}

async function buildAddProgressEntryChanges(input) {
  const text = requireSpanishText(input.text, "text");
  const initials = input.authorInitials ? String(input.authorInitials).trim() : "EBC";
  const entry = `* **${todayCrDate()}** — ${text} — ${initials}`;
  const progressPath = "05_progress/current.md";
  const progressText = await optionalText(progressPath);
  const changes = [
    {
      action: "update",
      path: progressPath,
      before: progressText,
      after: insertAfterHeading(
        updateLastUpdatedLine(progressText, `${todayCrDate()} CR (progreso actualizado)`),
        "## Completado en sesiones recientes",
        entry
      ),
    },
  ];

  if (input.component) {
    const component = requireEnum(input.component, "component", [
      "backend",
      "frontend",
      "database",
      "infrastructure",
      "documentation",
      "quality",
    ]);
    const componentPath = `05_progress/by-component/${component}.md`;
    const componentText = await optionalText(componentPath);
    changes.push({
      action: "update",
      path: componentPath,
      before: componentText,
      after: insertAfterHeading(
        updateLastUpdatedLine(componentText, `${todayCrDate()} CR (progreso actualizado)`),
        "## Completado",
        entry
      ),
    });
  }

  return changes;
}

async function buildCreateIssueChanges(input) {
  const title = requireSpanishText(input.title, "title");
  const severity = requireEnum(input.severity, "severity", ["critical", "high", "medium", "low"]);
  const component = requireString(input.component, "component");
  const symptom = requireSpanishText(input.symptom, "symptom");
  const rootCause = input.rootCause ? String(input.rootCause).trim() : "pendiente investigacion";
  const workaround = input.workaround ? String(input.workaround).trim() : "ninguno";
  const proposedFix = input.proposedFix ? String(input.proposedFix).trim() : "pendiente investigacion";
  const linkedTasks = Array.isArray(input.linkedTasks) && input.linkedTasks.length > 0 ? input.linkedTasks : ["ninguna"];
  const authorName = input.authorName ? String(input.authorName).trim() : "Ezequiel Baltodano Cubillo";
  const authorEmail = input.authorEmail ? String(input.authorEmail).trim() : "pendiente";
  const id = await nextIssueId();
  const issuePath = `07_issues/open/${id}.md`;
  const issueText = `# ${id} — ${title}

**Severidad:** ${severity}
**Estado:** abierto
**Componente:** ${component}
**Detectado:** ${currentCrTimestamp()}
**Autor:** ${authorName} \`<${authorEmail}>\`

---

## Síntoma

${symptom}

## Causa raíz

${rootCause}

## Workaround

${workaround}

## Fix propuesto

${proposedFix}

## Tareas vinculadas

${linkedTasks.map((task) => `* ${task}`).join("\n")}
`;

  const currentPath = "07_issues/current.md";
  const currentText = await optionalText(currentPath);
  const row = `| ${id} | ${severity} | ${component} | ${title} | ${linkedTasks.join(", ")} |`;
  const currentWithoutNone = currentText.replace(/\| — \| Ninguno activo \| — \| — \| — \|\n?/g, "");
  const updatedCurrent = insertTableRow(
    updateLastUpdatedLine(currentWithoutNone, `${todayCrDate()} CR (${id} creado)`),
    "# Issues activos",
    row
  );

  return [
    { action: "create", path: issuePath, after: issueText },
    { action: "update", path: currentPath, before: currentText, after: updatedCurrent },
  ];
}

async function buildCloseIssueChanges(input) {
  const id = sanitizeIssueId(requireString(input.id, "id")).toUpperCase();
  if (!/^ISSUE-\d{3,}$/.test(id)) {
    throw rpcError(-32602, `Issue id invalido: ${input.id}`);
  }

  const resolution = requireSpanishText(input.resolution, "resolution");
  const rootCause = requireSpanishText(input.rootCause, "rootCause");
  const learning = requireSpanishText(input.learning, "learning");
  const issuePath = `07_issues/open/${id}.md`;
  const issueText = await optionalText(issuePath);
  if (!issueText) {
    throw rpcError(-32602, `No existe el issue abierto ${id}`);
  }
  const component = input.progressComponent
    ? requireEnum(input.progressComponent, "progressComponent", [
        "backend",
        "frontend",
        "database",
        "infrastructure",
        "documentation",
        "quality",
      ])
    : progressComponentForIssue(issueText);

  const resolvedIssue = `${replaceIssueField(issueText, "Estado", "resuelto").trimEnd()}

## Resolución

${resolution}

## Causa raíz confirmada

${rootCause}

## Aprendizaje

${learning}

**Cerrado:** ${currentCrTimestamp()}
`;

  const currentPath = "07_issues/current.md";
  const currentText = await optionalText(currentPath);
  const updatedCurrent = updateLastUpdatedLine(
    removeTaskRows(currentText, id),
    `${todayCrDate()} CR (${id} resuelto)`
  );

  const archivePath = `07_issues/archive/${todayCrMonth()}.md`;
  const archiveText = await optionalText(archivePath);
  const archiveBase = archiveText || `# Issues resueltos — ${todayCrMonth()}\n`;
  const updatedArchive = `${archiveBase.trimEnd()}\n\n---\n\n${resolvedIssue}`;

  const progressPath = "05_progress/current.md";
  const progressText = await optionalText(progressPath);
  const progressEntry = `* **${todayCrDate()}** — ${id} resuelto: ${resolution}`;
  const updatedProgress = insertAfterHeading(
    updateLastUpdatedLine(progressText, `${todayCrDate()} CR (${id} resuelto)`),
    "## Completado en sesiones recientes",
    progressEntry
  );

  const changes = [
    { action: "update", path: currentPath, before: currentText, after: updatedCurrent },
    { action: archiveText ? "update" : "create", path: archivePath, before: archiveText || undefined, after: updatedArchive },
    { action: "update", path: progressPath, before: progressText, after: updatedProgress },
    { action: "delete", path: issuePath, before: issueText },
  ];

  if (component) {
    const componentPath = `05_progress/by-component/${component}.md`;
    const componentText = await optionalText(componentPath);
    changes.push({
      action: "update",
      path: componentPath,
      before: componentText,
      after: insertAfterHeading(
        updateLastUpdatedLine(componentText, `${todayCrDate()} CR (${id} resuelto)`),
        "## Completado",
        progressEntry
      ),
    });
  }

  return changes;
}

async function buildCreateDecisionChanges(input) {
  const title = requireSpanishText(input.title, "title");
  const domain = requireString(input.domain, "domain");
  const status = requireEnum(input.status ?? "aceptada", "status", ["propuesta", "aceptada", "reemplazada"]);
  const context = requireSpanishText(input.context, "context");
  const decision = requireSpanishText(input.decision, "decision");
  const reason = requireSpanishText(input.reason, "reason");
  const alternatives = Array.isArray(input.alternatives) && input.alternatives.length > 0 ? input.alternatives : ["Ninguna alternativa adicional documentada."];
  const consequences = Array.isArray(input.consequences) && input.consequences.length > 0 ? input.consequences : ["La decisión queda registrada para futuras sesiones de agentes."];
  const replaces = input.replaces ? String(input.replaces).trim() : "ninguno";
  const adrNumber = await nextAdrNumber();
  const id = `ADR-${String(adrNumber).padStart(2, "0")}`;
  const slug = slugify(title);
  const adrPath = `06_decisions/${id}-${slug}.md`;
  const displayStatus = capitalize(status);
  const adrText = `# ${id}: ${title}

**Fecha:** ${todayCrDate()}
**Estado:** ${status}
**Dominio:** ${domain}
**Reemplaza:** ${replaces}

## Contexto

${context}

## Decisión

${decision}

## Razón

${reason}

## Alternativas descartadas

${alternatives.map((item) => `* ${item}`).join("\n")}

## Consecuencias

${consequences.map((item) => `* ${item}`).join("\n")}
`;

  const indexPath = "06_decisions.md";
  const indexText = await optionalText(indexPath);
  const row = `| ${id} | ${title} | ${displayStatus} | ${domain} | ${todayCrDate()} | [\`${id}-${slug}.md\`](./06_decisions/${id}-${slug}.md) |`;
  const updatedIndex = insertTableRow(
    updateLastUpdatedLine(indexText, `${todayCrDate()} (${id})`),
    "## Índice de ADRs",
    row
  );

  return [
    { action: "create", path: adrPath, after: adrText },
    { action: "update", path: indexPath, before: indexText, after: updatedIndex },
  ];
}

async function validatePlannedChanges(changes) {
  const errors = [];
  const warnings = [];
  const allowedRoots = [
    "03_plan.md",
    "04_tasks/current.md",
    "04_tasks/tasks/",
    "04_tasks/done/",
    "05_progress/current.md",
    "05_progress/archive/",
    "05_progress/by-component/",
    "06_decisions.md",
    "06_decisions/",
    "07_issues/current.md",
    "07_issues/open/",
    "07_issues/archive/",
  ];

  for (const change of changes) {
    const normalized = normalizeRelativePath(change.path);
    const allowed = allowedRoots.some((root) => normalized === root || normalized.startsWith(root));
    if (!allowed) {
      errors.push(`Ruta no permitida para escritura: ${normalized}`);
    }

    if ((change.action === "create" || change.action === "update") && typeof change.after !== "string") {
      errors.push(`Cambio sin contenido after: ${normalized}`);
    }

    if (change.after) {
      for (const finding of scanTextForSecrets(change.after, normalized)) {
        errors.push(`Posible secret en cambio ${finding.path}:${finding.line} (${finding.pattern})`);
      }

      if (looksMostlyEnglish(change.after)) {
        warnings.push(`El contenido de ${normalized} podria no estar en espanol.`);
      }
    }
  }

  return {
    valid: errors.length === 0,
    errors,
    warnings,
  };
}

function readSpanishArrayOrDefault(value, name, fallback) {
  if (!Array.isArray(value) || value.length === 0) {
    return fallback;
  }

  return requireStringArray(value, name);
}

function readPlainArrayOrDefault(value, fallback) {
  if (!Array.isArray(value) || value.length === 0) {
    return fallback;
  }

  return value.map((item) => requireString(item, "likelyFiles[]"));
}

function normalizeRisk(value) {
  return requireEnum(String(value).trim().toLowerCase(), "risk", ["bajo", "medio", "alto"]);
}

function normalizeApproval(value) {
  const normalized = String(value).trim().toLowerCase();
  const map = {
    pendiente: "pendiente",
    aprobada: "aprobada",
    aprobado: "aprobada",
    approved: "aprobada",
    "no requerida": "no requerida",
    "not required": "no requerida",
  };

  if (!map[normalized]) {
    throw rpcError(-32602, `approval debe ser pendiente, aprobada o no requerida`);
  }

  return map[normalized];
}

function normalizeTaskStatus(value) {
  const normalized = String(value)
    .trim()
    .toLocaleLowerCase("es-CR")
    .normalize("NFKD")
    .replace(/\p{Diacritic}/gu, "")
    .replace(/[_-]+/g, " ");
  const map = {
    borrador: "Borrador",
    draft: "Borrador",
    lista: "Lista",
    ready: "Lista",
    pendiente: "Lista",
    "en progreso": "En progreso",
    "in progress": "En progreso",
    bloqueada: "Bloqueada",
    blocked: "Bloqueada",
    "en revision": "En revisión",
    review: "En revisión",
    completada: "Completada",
    done: "Completada",
    complete: "Completada",
  };

  if (!map[normalized]) {
    throw rpcError(-32602, `Estado de tarea no soportado: ${value}`);
  }

  return map[normalized];
}

function currentHeadingForStatus(status) {
  const map = {
    Borrador: "## Borradores (requieren aprobación antes de implementar)",
    Lista: "## Próximas (Lista / ready to start — en orden de implementación)",
    "En progreso": "## En progreso",
    "En revisión": "## En revisión",
  };

  return map[status] ?? "## Próximas (Lista / ready to start — en orden de implementación)";
}

function inspectTaskContract(taskText) {
  const title = taskText.match(/^#\s+TASK-[^\n]+—\s*(.+)$/m)?.[1]?.trim()
    ?? taskText.match(/^#\s+(.+)$/m)?.[1]?.trim()
    ?? "Sin titulo";
  const status = normalizeTaskStatus(readTaskField(taskText, "Estado") ?? "Borrador");
  const area = readTaskField(taskText, "Área") ?? readTaskField(taskText, "Area") ?? "DOC";
  const priority = readTaskField(taskText, "Prioridad") ?? "media";
  const risk = normalizeRisk(readTaskField(taskText, "Riesgo") ?? "medio");
  const approval = normalizeApproval(readTaskField(taskText, "Aprobación") ?? "pendiente");
  return {
    title,
    status,
    area,
    priority,
    risk,
    approval,
    likelyFiles: readBulletSection(taskText, "Archivos afectados / probables"),
    missing: requiredContractSections(taskText),
  };
}

function readTaskField(text, label) {
  const escaped = escapeRegExp(label);
  return text.match(new RegExp(`^\\*\\*${escaped}:\\*\\*\\s*(.+)$`, "m"))?.[1]?.trim();
}

function replaceTaskField(text, label, value) {
  const escaped = escapeRegExp(label);
  const pattern = new RegExp(`^\\*\\*${escaped}:\\*\\*\\s*.+$`, "m");
  if (!pattern.test(text)) {
    return text;
  }

  return text.replace(pattern, `**${label}:** ${value}`);
}

function replaceIssueField(text, label, value) {
  return replaceTaskField(text, label, value);
}

function progressComponentForIssue(text) {
  const value = String(readTaskField(text, "Componente") ?? "").toLowerCase();
  if (/\b(be|backend|api)\b/.test(value)) return "backend";
  if (/\b(fe|frontend|angular|spa)\b/.test(value)) return "frontend";
  if (/\b(db|database|base de datos|sql)\b/.test(value)) return "database";
  if (/\b(inf|infrastructure|infraestructura|deploy)\b/.test(value)) return "infrastructure";
  if (/\b(qa|quality|calidad|test)\b/.test(value)) return "quality";
  return "documentation";
}

function appendTaskNote(text, note) {
  const entry = `* ${note}`;
  if (text.includes("## Notas / contexto adicional")) {
    return insertAfterHeading(text, "## Notas / contexto adicional", entry);
  }

  return `${text.trimEnd()}\n\n## Notas / contexto adicional\n\n${entry}\n`;
}

function requiredContractSections(text) {
  const requiredFields = ["Estado", "Área", "Prioridad", "Riesgo", "Aprobación"];
  const requiredHeadings = [
    "## Contexto",
    "## Objetivo",
    "## Alcance permitido",
    "## Fuera de alcance",
    "## Criterios de aceptación",
    "## Pasos",
    "## Salida esperada",
    "## Validación",
    "## Rollback",
    "## Dependencias",
  ];
  const missing = [];

  for (const field of requiredFields) {
    if (!readTaskField(text, field)) {
      missing.push(`campo ${field}`);
    }
  }

  for (const heading of requiredHeadings) {
    if (!text.includes(heading)) {
      missing.push(heading);
    }
  }

  return missing;
}

function assertTaskContractComplete(contract) {
  if (contract.missing.length > 0) {
    throw rpcError(-32602, `La tarea no puede aprobarse; faltan: ${contract.missing.join(", ")}`);
  }
}

function assertTaskCanBeWorked(contract) {
  if (!["Lista", "En progreso"].includes(contract.status)) {
    throw rpcError(-32602, `work_task rechaza ${contract.status}; solo Lista o En progreso pueden trabajarse.`);
  }

  if (contract.risk === "alto" && contract.approval !== "aprobada") {
    throw rpcError(-32602, "work_task rechaza Riesgo: alto sin Aprobación: aprobada.");
  }
}

function assertTaskCanBeFinished(contract) {
  if (!["Lista", "En progreso", "En revisión"].includes(contract.status)) {
    throw rpcError(-32602, `No se puede cerrar una tarea ${contract.status}; debe estar Lista, En progreso o En revisión.`);
  }

  if (contract.risk === "alto" && contract.approval !== "aprobada") {
    throw rpcError(-32602, "No se puede cerrar Riesgo: alto sin Aprobación: aprobada.");
  }
}

function readBulletSection(text, heading) {
  const lines = text.split(/\r?\n/);
  const start = lines.findIndex((line) => line.trim() === `## ${heading}`);
  if (start === -1) {
    return [];
  }

  const items = [];
  for (let index = start + 1; index < lines.length; index += 1) {
    const line = lines[index];
    if (/^##\s+/.test(line)) {
      break;
    }

    const match = line.trim().match(/^\*\s+`?(.+?)`?$/);
    if (match) {
      items.push(match[1]);
    }
  }

  return items;
}

async function applyChanges(changes) {
  for (const change of changes) {
    const absolutePath = safeIaPath(change.path);
    if (change.action === "delete") {
      await fs.rm(absolutePath, { force: true });
      continue;
    }

    await fs.mkdir(path.dirname(absolutePath), { recursive: true });
    await fs.writeFile(absolutePath, ensureTrailingNewline(change.after), "utf8");
  }
}

function describeChange(change) {
  return {
    action: change.action,
    path: change.path,
    beforeChars: change.before?.length ?? 0,
    afterChars: change.after?.length ?? 0,
    diff: simpleDiff(change.before ?? "", change.after ?? "", change.path, change.action),
  };
}

async function nextTaskId(initials, area) {
  const files = await listMarkdownFiles(iaRoot, "");
  let max = 0;
  const pattern = new RegExp(`TASK-${escapeRegExp(initials)}-${escapeRegExp(area)}-(\\d+)`, "g");

  for (const file of files) {
    const text = await readText(safeIaPath(file));
    for (const match of text.matchAll(pattern)) {
      max = Math.max(max, Number.parseInt(match[1], 10));
    }
  }

  return `TASK-${initials}-${area}-${String(max + 1).padStart(2, "0")}`;
}

async function nextIssueId() {
  const files = await listMarkdownFiles(iaRoot, "");
  let max = 0;
  const pattern = /ISSUE-(\d+)/g;

  for (const file of files) {
    const text = await readText(safeIaPath(file));
    for (const match of text.matchAll(pattern)) {
      max = Math.max(max, Number.parseInt(match[1], 10));
    }
  }

  return `ISSUE-${String(max + 1).padStart(3, "0")}`;
}

async function nextAdrNumber() {
  const files = await listMarkdownFiles(safeIaPath("06_decisions"), "06_decisions");
  let max = 0;
  for (const file of files) {
    const match = path.basename(file).match(/^ADR-(\d+)/);
    if (match) {
      max = Math.max(max, Number.parseInt(match[1], 10));
    }
  }

  return max + 1;
}

  async function buildArchiveProgressChanges(input) {
    const keepDays = clampInteger(input.keepDays ?? 7, 0, 365);
    const currentPath = "05_progress/current.md";
    const currentText = await optionalText(currentPath);
    if (!currentText) return [];

    const cutoff = new Date();
    cutoff.setDate(cutoff.getDate() - keepDays);

    const sectionRe = /(## Completado en sesiones recientes\s*\n)([\s\S]*?)(?=\n## |\n# |$)/;
    const sectionMatch = currentText.match(sectionRe);
    if (!sectionMatch) return [];

    const sectionHeader = sectionMatch[1];
    const sectionBody = sectionMatch[2];
    const sectionOffset = currentText.indexOf(sectionMatch[0]);

    const entryRe = /^(\* \*\*(\d{4}-\d{2}-\d{2})\*\*[^\n]*(?:\n(?!\* \*\*\d{4}-\d{2}-\d{2}\*\*|\n## |\n# ).*)*)/gm;
    const toArchive = new Map();
    const toKeep = [];

    for (const m of sectionBody.matchAll(entryRe)) {
      const entry = m[1].trimEnd();
      const entryDate = new Date(`${m[2]}T00:00:00`);
      if (entryDate < cutoff) {
        const month = m[2].slice(0, 7);
        if (!toArchive.has(month)) toArchive.set(month, []);
        toArchive.get(month).push(entry);
      } else {
        toKeep.push(entry);
      }
    }

    if (toArchive.size === 0) return [];

    const changes = [];

    const newBody = toKeep.length > 0 ? toKeep.join("\n\n") + "\n" : "\n";
    const newSection = sectionHeader + newBody;
    const newCurrent = currentText.slice(0, sectionOffset) + newSection + currentText.slice(sectionOffset + sectionMatch[0].length);
    changes.push({ action: "update", path: currentPath, before: currentText, after: ensureTrailingNewline(newCurrent) });

    for (const [month, entries] of [...toArchive.entries()].sort()) {
      const archivePath = `05_progress/archive/${month}.md`;
      const existing = await optionalText(archivePath);
      const block = entries.join("\n\n");
      const after = existing
        ? ensureTrailingNewline(existing.trimEnd() + "\n\n" + block)
        : ensureTrailingNewline(`# Progreso archivado — ${month}\n\n${block}`);
      changes.push({ action: existing ? "update" : "create", path: archivePath, before: existing || undefined, after });
    }

    return changes;
  }

  return {
    previewOperation,
    runWriteOperation,
    workTask,
  };
}
