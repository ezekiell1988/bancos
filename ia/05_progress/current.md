# Progreso actual

> **Última actualización:** 2026-10-23 CR
> **Fase activa:** Fase 2 — Carga y cierres en MCP (2 de 4 componentes completados)

<!-- El MCP iaWorkflow mantiene las dos líneas de arriba, «Completado en sesiones recientes» y
«En progreso». A mano solo: Resumen ejecutivo, Pendiente y Riesgos y bloqueos. -->

## Resumen ejecutivo

`Bancos.Mcp` es el único proyecto activo. La Fase 2 — Carga y cierres en MCP lleva 2 de 4 componentes; las filas «Tools MCP de extracción por formato» e «Idempotencia e importación con huella» no tienen tarea propia y deben desglosarse con `create_task` + `assign_task_to_phase` antes de cerrar la fase. En curso solo está TASK-EBC-INF-13 (proyecto personal, fuera del alcance financiero).

## Completado en sesiones recientes

* **2026-10-23** — IA: Estructura de `03_plan.md` y `05_progress/` alineada al estándar iaWorkflow: sección `## Fases`, `## Pendiente de planificación`, filas de la Fase 2 con su TASK-ID cuando la tarea existe (TASK-EBC-DB-07, TASK-EBC-MCP-31), secciones canónicas de progreso y prompts regenerados en `ia/prompts/`.

* **2026-09-25** — TASK-EBC-DOC-16: Se adoptaron las reglas de workflow en ia/rules: init-ia.mjs --refresh-rules creó README.md, planificacion.md, gestion-tareas.md, revision-codigo.md y cierre-sesion.md, idénticas a las plantillas de create-ia-workflow. Los encabezados de checklist-revision.md y mapa-area-skill.md ahora apuntan a revision-codigo.md y gestion-tareas.md. Se reemplazaron las referencias vivas a skills de workflow en TASK-EBC-DOC-15 y en .github/prompts, y ia/README.md incluye el índice de reglas por intención. El grep de los skills retirados solo encuentra esta tarea e ia_validate es válido.

* **2026-09-25** — TASK-EBC-DOC-17: 05_progress/current.md bajo el límite (2287 caracteres antes de este cierre): viñetas de «Completado en sesiones recientes» fechadas (cierre de la tarea en 04_tasks/done o git blame de la línea), archive_progress por antigüedad y recorte manual de lo más viejo a 05_progress/archive/AAAA-MM.md; se conservan las entradas más recientes.

* **2026-09-25** — TASK-EBC-DOC-18: El repo conserva solo sus skills propios bccr-sdde (tipo de cambio BCCR) y llm-audit-db (auditoría LLM en SQL, usada por LlmAuditService); se borraron 51 skills en .agents, .claude y .codex (11 duplicaban globales y el resto eran copias de otros proyectos). Sin referencias vivas a los borrados. Decisión del usuario 2026-09-25.

* **2026-09-25** — TASK-EBC-DOC-15: ia/rules/mapa-area-skill.md completo: 7 áreas (MCP, BE, DB, INF, QA, DOC, ARCH) con skill principal y skills por dominio; propios bccr-sdde y llm-audit-db, globales mcp-vscode-dotnet, dotnet-10-csharp-14, mssql-ef-best-practices, llm-audit-dotnet, ia-workflow. FE sin frontend activo. Sin marcadores POR DEFINIR.

* **2026-09-16** — TASK-EBC-DOC-14 cerrada: Se sincronizó mcp-vscode-dotnet desde la base canónica .agents/skills hacia .claude/skills y .codex/skills. La verificación posterior reportó 60 skills idénticos, sin conflictos. — EBC

## En progreso

| Item | Responsable | Estado | Siguiente paso |
|------|-------------|--------|----------------|
| TASK-EBC-INF-13 Video regalo: muñeca 3D tierna (imagen Azure AI Foundry + Veo 3.1) | Ezequiel Baltodano Cubillo | En progreso | Continuar la implementación |

## Pendiente

| Prioridad | Item | Razón |
|-----------|------|-------|
| 1 | Crear tarea para «Tools MCP de extracción por formato» (Fase 2) | Fila sin TASK-ID marcada «En revisión»; ninguna tool la puede cerrar y bloquea el cierre de la fase |
| 2 | Crear tarea para «Idempotencia e importación con huella» (Fase 2) | Fila sin TASK-ID; bloquea el cierre de la fase |
| 3 | Decidir TASK-EBC-DB-05 (estado legado «En revisión») | El estado ya no existe en el workflow: volver a Borrador o cerrar |
| 4 | Revisar estado de las Fases 3 y 4 en el plan | Hay tareas cerradas de clasificación y reportes (p. ej. TASK-EBC-DB-11, TASK-EBC-MCP-33, TASK-EBC-MCP-34, TASK-EBC-MCP-39) y las fases siguen sin filas con TASK-ID |
| 5 | Completar preguntas y descubrimiento de requisitos financieros | Arrastrado desde la creación de `/ia` (2026-07-18) |

## Riesgos y bloqueos

| Riesgo | Impacto | Mitigación |
|--------|---------|------------|
| La Fase 2 tiene filas sin TASK-ID | No puede cerrarse con `finish_task` ni generar sus flags de revisión | Desglosarlas con `create_task` + `assign_task_to_phase` |
| TASK-EBC-MCP-18 está definido dos veces en `04_tasks/done/2026-07.md` (auditoría de préstamos y cierres por período) | Lecturas por ID ambiguas | No usarlo como fila del plan; corregir el ID duplicado en una tarea DOC |
| Fallos preexistentes de la suite global (inicialización de Hangfire y conteo de datos semilla), anotados en TASK-EBC-MCP-41 | Pruebas globales no confiables | Tratarlos en una tarea QA |
