# 05 — Progreso Actual

> Última actualización: 2026-10-25 20:00 CR
> Fase activa: Fase 3 — Skill para anotar documentos (0 de 1 componentes completados)

<!-- Con el MCP iaWorkflow, las dos líneas de arriba, «Completado en sesiones recientes» y la tabla de
«En progreso» las mantienen work_task, finish_task, close_issue e ia_add_progress_entry: no editarlas a
mano. A mano solo: Resumen ejecutivo, Pendiente y Riesgos y bloqueos. -->

## Resumen ejecutivo

Proyecto reiniciado como registro sencillo de movimientos en jsonl (`datos/`, fuera de git). `/ia` nueva creada con create-ia-workflow. Próxima prioridad: MCP `bancos` (TASK-EBC-MCP-01) y luego el skill para anotar documentos (TASK-EBC-DOC-04).

## Completado en sesiones recientes

* **2026-10-25** — TASK-EBC-MCP-01: MCP bancos con 6 tools (tipos, agregar tipo, registrar con respaldo y sin duplicados, movimientos, resumen, borrar), smoke 24/24 OK sobre carpeta temporal y registrado en Claude Code, VS Code y Codex.
* **2026-10-25** — TASK-EBC-DOC-03: /ia completada para el registro sencillo en jsonl: contexto, 6 requisitos, arquitectura, fases 2 (MCP bancos) y 3 (skill), progreso por componente; reglas de CLAUDE.md/AGENTS.md pasadas a 00_context y esos archivos retirados.
* **2026-10-25** — TASK-EBC-DOC-02: Mapa de área a skill: MCP → mcp-vscode, SKILL → create-skill, DOC → ia-workflow.
* **2026-10-25** — TASK-EBC-DOC-01: Checklist de revisión propio: privacidad del repo público, formato de datos/, contrato del MCP bancos (mcp-vscode), skills (create-skill) y trazabilidad.

## En progreso

| Item | Responsable | Estado | Siguiente paso |
|------|-------------|--------|----------------|
| TASK-EBC-DOC-04 Skill para anotar documentos | Ezequiel Baltodano Cubillo | En progreso | Crear el skill con scripts/new-skill.mjs de create-skill. |

## Pendiente

| Prioridad | Item | Razón |
|-----------|------|-------|
| Alta | TASK-EBC-MCP-01 MCP bancos | Sin él los movimientos se anotan a mano |
| Media | TASK-EBC-DOC-04 Skill para anotar documentos | Depende del MCP |

## Riesgos y bloqueos

| Riesgo | Impacto | Mitigación |
|--------|---------|------------|
| Repo público con datos financieros | Alto | `datos/` y `.local-secrets/` en `.gitignore`; checklist de revisión |
