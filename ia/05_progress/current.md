# Progreso actual

> **Última actualización:** 2026-09-25 CR
> **Fase activa:** Fase 2 — Carga y cierres en MCP (2 de 4 componentes completados)

## En progreso

* Descubrimiento de requisitos financieros.
* Firmas de siete plantillas documentadas; faltan validación de XLS binario y semántica de CSV de crédito durante implementación.

| Item | Responsable | Estado | Siguiente paso |
|------|-------------|--------|----------------|
| TASK-EBC-INF-13 Video regalo: muñeca 3D tierna (imagen Azure AI Foundry + Veo 3.1) | Ezequiel Baltodano Cubillo | En progreso | Continuar la implementación |

## Completado en sesión actual

* Creado `/ia`: contexto, requisitos, arquitectura, plan, tareas, progreso, ADRs, issues, retrospectiva, templates, prompts y skills de workflow.
* Inspeccionados formatos de primera carga de forma anonimizada y documentados detectores/validaciones.
* Configurados MCP `iaWorkflow` y `dbquery`; smoke tests completos y configuración de Codex/VS Code/Claude actualizada.

## Pendiente

* Completar preguntas de requisitos.
* Auditar estructura `/ia`.
* Abrir una sesión nueva de Codex para cargar MCP nativos.
* Aprobar y ejecutar `TASK-EZ-BE-01` mediante `iaWorkflow`.

## Completado en sesiones recientes

* **2026-09-16** — TASK-EBC-DOC-14 cerrada: Se sincronizó mcp-vscode-dotnet desde la base canónica .agents/skills hacia .claude/skills y .codex/skills. La verificación posterior reportó 60 skills idénticos, sin conflictos. — EBC

* **2026-09-25** — TASK-EBC-DOC-16: Se adoptaron las reglas de workflow en ia/rules: init-ia.mjs --refresh-rules creó README.md, planificacion.md, gestion-tareas.md, revision-codigo.md y cierre-sesion.md, idénticas a las plantillas de create-ia-workflow. Los encabezados de checklist-revision.md y mapa-area-skill.md ahora apuntan a revision-codigo.md y gestion-tareas.md. Se reemplazaron las referencias vivas a skills de workflow en TASK-EBC-DOC-15 y en .github/prompts, y ia/README.md incluye el índice de reglas por intención. El grep de los skills retirados solo encuentra esta tarea e ia_validate es válido.

* **2026-09-25** — TASK-EBC-DOC-17: 05_progress/current.md bajo el límite (2287 caracteres antes de este cierre): viñetas de «Completado en sesiones recientes» fechadas (cierre de la tarea en 04_tasks/done o git blame de la línea), archive_progress por antigüedad y recorte manual de lo más viejo a 05_progress/archive/AAAA-MM.md; se conservan las entradas más recientes.
