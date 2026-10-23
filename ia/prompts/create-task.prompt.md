---
description: Convertir un requisito o item del plan en una tarea accionable con la regla ia/rules/gestion-tareas.md.
---

## Leer

* Con el MCP `iaWorkflow`: `ia_validate` e `ia_get_context` con `intent: "planificar"`.
* Sin MCP (fallback): `ia/03_plan.md`, `ia/04_tasks/current.md`.
* Regla del proyecto: `rules/gestion-tareas.md` (con el MCP, `ia_inspect` `action: "read_file"`; sin MCP, `ia/rules/`).

## Objetivo

Crear una tarea `Borrador` bien delimitada a partir de un requisito, item de plan o idea de backlog.

## Reglas

* Seguir `ia/rules/gestion-tareas.md`: `create_task` en preview y luego con `apply: true`.
* No crear la tarea sin alcance in/out, riesgo y criterios de aceptación verificables; el primer paso de `steps` debe ser una acción concreta (se publica como «Siguiente paso» en `05_progress`).
* Revisar los `warnings` y `undefinedTaskReferences` del preview: corregir o crear antes la tarea citada; no citar TASK-IDs futuros para reservar su número.
* Si pertenece a una fase, asignarla con `assign_task_to_phase` (una fila `⏳ TASK-ID` por tarea) en vez de editar `03_plan.md`.

## Salida esperada

Una tarea nueva en `Borrador`, registrada en `04_tasks/current.md` y, si aplica, asignada a su fase.
