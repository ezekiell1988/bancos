---
description: Implementar la siguiente tarea Lista usando work_task y el skill de área correspondiente.
---

## Leer

* Con el MCP `iaWorkflow`: `ia_validate` e `ia_get_context` con `intent: "implementar"`; la tarea (`read_task`) con `ia_inspect`.
* Sin MCP (fallback): `ia/00_context.md`, `ia/02_architecture.md`, la tarea en `ia/04_tasks/tasks/`.
* Reglas del proyecto: `rules/gestion-tareas.md` y `rules/mapa-area-skill.md` (con el MCP, `ia_inspect` `action: "read_file"`; sin MCP, `ia/rules/`).

## Objetivo

Implementar una tarea `Lista` seleccionada hasta cumplir sus criterios de aceptación.

## Reglas

* Si aún no hay tarea elegida, seleccionarla con el criterio de `ia/rules/planificacion.md` (una sola por sesión).
* Seguir el ciclo de vida de `ia/rules/gestion-tareas.md`. Solo implementar tareas en estado `Lista`; nunca `Borrador`.
* Iniciar con `work_task` antes de editar cualquier archivo; acepta `nextStep` (si no, el primer paso de `## Pasos` se publica como «Siguiente paso» en `05_progress`).
* Cargar el skill técnico del área según `ia/rules/mapa-area-skill.md` antes de codificar.
* Si se bloquea por una dependencia externa, usar `work_task` con `transition: "blocked"`, `reason` y, si cambia, `nextStep`.
* No editar a mano `03_plan.md` ni las partes gestionadas de `05_progress/current.md`: las mantienen `work_task` y `finish_task`.

## Salida esperada

Los criterios de aceptación de la tarea cumplidos y verificables, lista para revisión.
