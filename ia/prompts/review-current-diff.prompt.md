---
description: Revisar el conjunto de cambios actual con la regla ia/rules/revision-codigo.md.
---

## Leer

* Con el MCP `iaWorkflow`: `ia_validate` e `ia_get_context` con `intent: "revisar"`; la tarea de origen y los ADRs pertinentes con `ia_inspect`.
* Sin MCP (fallback): `ia/02_architecture.md`, `ia/06_decisions.md`, `ia/07_issues.md`, la tarea de origen del cambio, si aplica.
* Reglas del proyecto: `rules/revision-codigo.md` y `rules/checklist-revision.md` (con el MCP, `ia_inspect` `action: "read_file"`; sin MCP, `ia/rules/`).

## Objetivo

Emitir un veredicto de revisión sobre el diff actual usando el checklist del proyecto.

## Reglas

* Seguir `ia/rules/revision-codigo.md` y aplicar cada punto de `ia/rules/checklist-revision.md`; no corregir el código silenciosamente.
* Clasificar cada hallazgo en bloqueante, riesgo o mejora.
* Reportar como hallazgo cualquier edición manual de partes de `/ia` que mantiene el MCP (filas `TASK-ID`, `> Avance` y estados de fase en `03_plan.md`; encabezado, `En progreso` y completado reciente en `05_progress/current.md`; `04_tasks/current.md`).
* Nunca citar valores de secretos; reportar solo su ubicación.

## Salida esperada

Un reporte con hallazgos clasificados y un veredicto final.
