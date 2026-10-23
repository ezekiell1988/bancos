---
description: Determinar si un issue invalida el cierre de una tarea archivada y, si aplica, reabrirla.
---

## Leer

* Con el MCP `iaWorkflow`: `ia_validate` e `ia_get_context` con `intent: "depurar"`; el issue y la tarea archivada (`read_task`) con `ia_inspect`.
* Sin MCP (fallback): el issue abierto en `ia/07_issues/open/`, la tarea archivada en `ia/04_tasks/done/`.

## Objetivo

Decidir si el cierre original de la tarea sigue siendo válido y actuar en consecuencia.

## Reglas

* Usar `reopen_task` solo si el issue demuestra que el cierre original era incorrecto.
* Si el cierre sigue siendo válido, crear una tarea de corrección nueva enlazada al issue y a la tarea histórica en vez de reabrir.

## Salida esperada

Una decisión documentada y, si aplica, la tarea reabierta: vuelve a `Borrador`, su fila y su fase se reabren en `03_plan.md` y queda enlazada al issue (en un solo commit; `05_progress` la mostrará al iniciarla de nuevo).
