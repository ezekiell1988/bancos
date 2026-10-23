# Prompts de workflow

Generados con `init-ia.mjs --refresh-prompts` (no editar a mano; regenerar). Cada prompt usa primero `ia_validate` e `ia_get_context` del MCP `iaWorkflow` y sigue la regla de `ia/rules/` que le corresponde.

* `create-spec.prompt.md`: necesidad a requisito en `01_requirements.md`.
* `create-task.prompt.md`: requisito o item del plan a tarea (`rules/gestion-tareas.md`).
* `implement-next-task.prompt.md`: implementar la siguiente tarea `Lista` con `work_task`.
* `review-current-diff.prompt.md`: revisar cambios (`rules/revision-codigo.md`).
* `debug-known-issue.prompt.md`: investigar un issue de `07_issues`.
* `reopen-task-from-issue.prompt.md`: decidir si un issue reabre una tarea archivada.
* `close-session.prompt.md`: cerrar la sesión (`rules/cierre-sesion.md`).
* `record-retrospective-actions.prompt.md`: retrospectiva de una fase cerrada y sus acciones.
* `create-skill-from-retrospective.prompt.md`: aprendizaje repetible a skill del proyecto.
