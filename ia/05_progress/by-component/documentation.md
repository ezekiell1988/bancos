> **Última actualización:** 2026-07-24 CR (TASK-EBC-MCP-18 completada)



## Completado

* **2026-07-24** — TASK-EBC-MCP-18: Auditoría completada sin defectos. Parser MCP extrae campos de encabezado completos (monto original, tasa, plazo, fecha inicio) más historial de pagos y tabla de cuotas. Job hace upsert correcto de header y cuotas, calcula porciones corriente y largo plazo. Sin incidencias que abrir. — EBC

* **2026-07-22** — TASK-EBC-MCP-09: Implementado endpoint SSE en /mcp/sse para Claude Code. Descubierto que la spec MCP requiere camelCase en tools/list — PascalCase de .NET impedía el descubrimiento. Corregidos regexes del parser Coopealianza (espacios opcionales en texto PDF sin separadores). Verificado: 4 PDFs paginados → 36 cuotas en tbLoanPayments, LoanStatement con datos de header completos. Documentado en references/10-sse-claude-code.md. — EBC

* **2026-07-21** — TASK-EBC-DOC-05: Documentada en ia/README.md la convención para ubicar tools MCP por feature. — EBC

* **2026-07-20** — TASK-EBC-DOC-04: Se actualizó la documentación para distinguir Bancos.Api como monolito funcional y Bancos.Mcp como servidor MCP auxiliar independiente, con catálogo, migraciones y base de datos propios. — EBC

* **2026-07-20** — TASK-EBC-MCP-01: Se creó el servidor MCP independiente para Copilot Studio con transporte JSON-RPC, tool diagnóstica segura, HTTPS local, pruebas y documentación. — EBC

* **2026-07-18** — TASK-EBC-DOC-03: Se sincronizaron las skills canónicas desde .agents hacia .claude y .codex; el reporte final confirma 56 skills idénticas. — EBC

* **2026-07-18** — TASK-EBC-DOC-02: Se creó la skill angular-css-architecture con la convención de tokens globales, styleUrl por componente y validación responsive. — EBC

* **2026-07-18** — TASK-EBC-DOC-01: ADR-02 y 00_context.md actualizados para que activos y pasivos USD generen diferencial cambiario. — EBC
