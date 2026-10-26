# 05 — Progreso Actual

> Última actualización: 2026-10-26 08:06 CR
> Fase activa: Fase 6 — Facturas con varios pagos (0 de 1 componentes completados)

<!-- Con el MCP iaWorkflow, las dos líneas de arriba, «Completado en sesiones recientes» y la tabla de
«En progreso» las mantienen work_task, finish_task, close_issue e ia_add_progress_entry: no editarlas a
mano. A mano solo: Resumen ejecutivo, Pendiente y Riesgos y bloqueos. -->

## Resumen ejecutivo

Proyecto reiniciado como registro sencillo de movimientos en jsonl (`datos/`, fuera de git). `/ia` nueva creada con create-ia-workflow. Próxima prioridad: MCP `bancos` (TASK-EBC-MCP-01) y luego el skill para anotar documentos (TASK-EBC-DOC-04).

## Completado en sesiones recientes

* **2026-10-26** — TASK-EBC-DOC-04: Skill record-movements probado con documentos reales: 2 HTML de movimientos del BCR y 2 estados de la BAC (404 movimientos) más 15 comprobantes SINPE. Su versión por niveles quedó en TASK-EBC-DOC-06.
* **2026-10-26** — TASK-EBC-DOC-06: Skill record-movements reescrito por niveles (qué documento va a cada nivel y con qué tool), rubros hogar/papá, vínculos y pendientes, reportes (todo, sin papá, solo papá) y bancos_sql. datos/reglas.md ajustado a «hogar». 00_context y 02_architecture al día.
* **2026-10-26** — TASK-EBC-MCP-03: datos/ migrado con respaldo previo: 404 movimientos con id, 389 con referencia sacada del HTML BCR y PDF BAC (los 15 sin referencia son intereses y cuotas, que no la traen); 15 fotos de SINPE pasadas a comprobantes pendientes con su número de documento. El nivel 1 solo bajó lo de esas fotos (oct BCR). SQLite reconstruida.
* **2026-10-26** — TASK-EBC-MCP-02: MCP bancos 2.0: movimientos con id y referencia; comprobantes (nivel 2) y facturas (nivel 3) con vínculo automático y manual; resumen por rubro (hogar, papa) con excluirDe/de/excluirTipos; pendientes; SQLite derivado con bancos_sql. Smoke: 61 OK.
* **2026-10-26** — EBC: MCP bancos: `fecha` pasa a ser la fecha de proceso del banco y se agrega `fechaReal` de referencia; registrar reubica anotados con fecha provisional; editar acepta fechaReal. Datos migrados (419 movimientos) con respaldo en datos/respaldos/. Smoke OK.
* **2026-10-25** — TASK-EBC-MCP-01: MCP bancos con 6 tools (tipos, agregar tipo, registrar con respaldo y sin duplicados, movimientos, resumen, borrar), smoke 24/24 OK sobre carpeta temporal y registrado en Claude Code, VS Code y Codex.
* **2026-10-25** — TASK-EBC-DOC-03: /ia completada para el registro sencillo en jsonl: contexto, 6 requisitos, arquitectura, fases 2 (MCP bancos) y 3 (skill), progreso por componente; reglas de CLAUDE.md/AGENTS.md pasadas a 00_context y esos archivos retirados.
* **2026-10-25** — TASK-EBC-DOC-02: Mapa de área a skill: MCP → mcp-vscode, SKILL → create-skill, DOC → ia-workflow.
* **2026-10-25** — TASK-EBC-DOC-01: Checklist de revisión propio: privacidad del repo público, formato de datos/, contrato del MCP bancos (mcp-vscode), skills (create-skill) y trazabilidad.

## En progreso

| Item | Responsable | Estado | Siguiente paso |
|------|-------------|--------|----------------|
| TASK-EBC-MCP-04 Pagos de facturas en el MCP | Ezequiel Baltodano Cubillo | En progreso | Cambiar vinculos.mjs y bancos_factura para comprobantes en lista |

## Pendiente

| Prioridad | Item | Razón |
|-----------|------|-------|
| Alta | TASK-EBC-MCP-01 MCP bancos | Sin él los movimientos se anotan a mano |
| Media | TASK-EBC-DOC-04 Skill para anotar documentos | Depende del MCP |

## Riesgos y bloqueos

| Riesgo | Impacto | Mitigación |
|--------|---------|------------|
| Repo público con datos financieros | Alto | `datos/` y `.local-secrets/` en `.gitignore`; checklist de revisión |
