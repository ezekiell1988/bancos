# Progreso actual

> **Última actualización:** 2026-07-31 CR (TASK-EBC-MCP-53 completada)

## En curso

* Descubrimiento de requisitos financieros.
* Firmas de siete plantillas documentadas; faltan validación de XLS binario y semántica de CSV de crédito durante implementación.

## Completado en sesión actual

* Creado `/ia`: contexto, requisitos, arquitectura, plan, tareas, progreso, ADRs, issues, retrospectiva, templates, prompts y skills de workflow.
* Inspeccionados formatos de primera carga de forma anonimizada y documentados detectores/validaciones.
* Configurados MCP `iaWorkflow` y `dbquery`; smoke tests completos y configuración de Codex/VS Code/Claude actualizada.

## Próximo

* Completar preguntas de requisitos.
* Auditar estructura `/ia`.
* Abrir una sesión nueva de Codex para cargar MCP nativos.
* Aprobar y ejecutar `TASK-EZ-BE-01` mediante `iaWorkflow`.

## Completado en sesiones recientes



* **2026-07-31** — TASK-EBC-MCP-53 cerrada: Se ajustaron los reportes de estado de resultados y situación financiera para recibir un único periodId, devolver TOON como contenido principal y comparar automáticamente contra el período anterior cuando existe. Se corrigió el cálculo de cierres para procesar desde el período solicitado, conservar saldos de cuentas sin movimientos y usar un cron mensual válido. Se añadieron pruebas de comparación, variaciones y arrastre de saldos. — GC

* **2026-07-29** — TASK-EBC-MCP-36 cerrada: Se completó el seguimiento de importaciones apoyándose en el propio almacenamiento de Hangfire (sin tabla nueva): process_import_file ahora devuelve en TOON archivo+jobId+estado; ImportFileJob.ExecuteAsync retorna un resumen (Task&lt;string&gt;) que Hangfire guarda como Result del job, visible junto al historial de estados. Se agregaron tres tools nuevas en Features/Imports/: get_import_job_status (estado, resumen o error con mensaje/detalle, siguiente paso), list_recent_import_jobs (TOON, en cola/procesando/completado/error, incluye duplicados detectados vía el resumen 'Duplicado detectado…') y retry_import_job (solo permite reintentar jobs en estado 'error', reencola con los mismos identificadores sin bytes; seguro porque ExecuteAsync solo llama SaveChangesAsync al final). — EBC

* **2026-07-29** — TASK-EBC-MCP-45 cerrada: Se completaron las confirmaciones pendientes de los 138 movimientos autorizados por los cinco patrones (tarjeta, préstamo, servicios, transporte, alimentación). El flujo estándar de clasificación (TASK-EBC-MCP-46) terminó de vaciar la cola de pendientes hasta 0 movimientos. Se regeneró docs/movimientos-pendientes-clasificacion.md, que ahora muestra Total: 0 sin exponer IDs internos ni datos financieros adicionales. — EBC

* **2026-07-29** — TASK-EBC-MCP-35 (post-cierre): se agregó filtro por categoría (categoryId) en search_transactions, usando la clasificación más reciente de cada movimiento. Actualizados TransactionsQueryService, SearchTransactionsTool y pruebas (77/77 exitosas). — EBC

* **2026-07-29** — TASK-EBC-MCP-35 cerrada: Se agregaron 4 tools MCP de solo lectura: list_bank_accounts, list_periods, search_transactions y get_transaction_detail, con paginación estable y sin exponer IBAN, número de tarjeta ni credenciales. — EBC

* **2026-07-29** — TASK-EBC-MCP-34 cerrada: Se agregó el feature Reports en Bancos.Mcp: ReportingService calcula estado de resultados (ingresos/gastos por categoría, resultado neto, movimientos pendientes de clasificar) y situación financiera (activos/pasivos derivados de tbAccountPeriodClosings, capital como residuo activos-pasivos, cuentas sin cierre calculado). ReportHtmlRenderer genera HTML autocontenido con período, moneda CRC, fecha de generación y advertencias, escapando todo texto dinámico con WebUtility.HtmlEncode. Se expusieron dos tools MCP: get_income_statement_report y get_balance_sheet_report, registradas en ReportsModule y en Program.cs. De paso se corrigió un test preexistente roto (UnclassifiedTransactionsMarkdownExporterTests) que no compilaba por un cambio de firma ya aplicado en ClassificationService/UnclassifiedTransactionSummary (feature Classification, fuera de esta tarea) para poder validar el suite completo. — EBC

* **2026-07-29** — TASK-EBC-MCP-46 cerrada: Flujo estándar de clasificación manual asistida por MCP implementado y ejecutado exitosamente. Se clasificaron las 159 transacciones pendientes hasta llegar a 0. Nuevo formato de MD con banco/cuenta/ID, tool apply_classifications_from_markdown, mapper NoteToCategory con keywords, y parámetro sortBy en el export. — EB

* **2026-07-29** — ## Avance TASK-EBC-MCP-46 — Flujo estándar de clasificación manual asistida por MCP

### Lo implementado

**Exportador (`UnclassifiedTransactionsMarkdownExporter`):**
- Nuevo formato de columnas: `| ID | Fecha | Banco | Cuenta | Descripción | Importe | Moneda | Nota |`
- Eliminado Ref. (ya no se necesita, el ID es el identificador)
- Eliminada columna "Categoría propuesta" — el usuario solo llena "Nota"
- Agregadas columnas Banco y Cuenta (nombre del banco + código de cuenta)
- Ordenado por moneda (CRC primero, luego USD) y luego importe valor absoluto descendente
- Sección "Cómo completar" actualizada con instrucciones del nuevo flujo

**Parser (`MarkdownClassificationParser`):**
- Parsea el nuevo formato sin Ref., usando UUID en columna ID y texto libre en Nota

**Mapper de notas a categorías (`NoteToCategory`):**
- Convierte texto libre de la nota a código de categoría por palabras clave
- Cubre: traslados, pago tarjeta, préstamos, salario, alimentación, transporte, vivienda, servicios, salud, entretenimiento, otros ingresos

**Tool MCP nueva (`apply_classifications_from_markdown`):**
- Lee el MD, mapea notas a categorías, llama `ConfirmManualClassificationAsync` internamente
- Retorna: aplicadas, omitidas, no resueltas (para que Claude las clasifique individualmente)

**`ClassificationService`:**
- `ListUnclassifiedAsync` ahora incluye `BankName` y `AccountCode` vía JOIN a BankAccounts y Banks
- Nuevo método `GetCategoriesAsync` para listar categorías disponibles
- Ordenamiento por moneda y luego importe absoluto descendente

**`ClassificationModule`:** registra la nueva tool `ApplyClassificationsFromMarkdownTool`

### Flujo estándar establecido

1. Llamar `export_unclassified_transactions_markdown` → genera/actualiza el MD
2. Usuario abre el MD y llena columna **Nota** en los movimientos que reconoce
3. Decirle a Claude "aplica" → llama `apply_classifications_from_markdown`
4. Los no resueltos por keywords los clasifica Claude individualmente con `confirm_transaction_classification`
5. Regenerar el MD para la siguiente ronda

### Nota operativa
**Cada vez que Claude haga cambios en Bancos.Mcp, el usuario debe correr `.mcp/bancos-mcp.ps1` para que el servidor MCP tome los cambios compilados.** El script mata el proceso en puerto 8000, levanta Docker si no está corriendo, aplica migraciones y reinicia con `dotnet watch run`. — EBC

* **2026-07-27** — TASK-EBC-DOC-13 cerrada: Se creó una plantilla Power Query segura para ejecutar SELECTs parametrizados contra SQL Server. — EBC

* **2026-07-27** — TASK-EBC-MCP-52 cerrada: Se aplicaron 18 clasificaciones inequívocas de la nueva tanda y el tool MCP regeneró el Markdown con 159 pendientes. — EBC

* **2026-07-27** — TASK-EBC-MCP-51 cerrada: Se aplicaron 67 clasificaciones inequívocas de la nueva tanda de notas y el tool MCP regeneró el Markdown con 177 pendientes. — EBC

* **2026-07-27** — TASK-EBC-MCP-50 cerrada: Se identificaron y confirmaron 2 coincidencias adicionales de alta confianza y el tool MCP regeneró el Markdown con 244 pendientes. — EBC

* **2026-07-27** — TASK-EBC-MCP-49 cerrada: Se añadió Place a las reglas de clasificación, se propagó a movimientos futuros, se aplicaron 66 notas con lugares conocidos y se regeneró el Markdown a 246 pendientes. — EBC

* **2026-07-27** — TASK-EBC-MCP-48 cerrada: Se confirmaron los 69 movimientos que coincidían con patrones reutilizables y el tool MCP regeneró el documento con 312 pendientes. — EBC

* **2026-07-27** — TASK-EBC-MCP-47 cerrada: Se aplicaron 29 clasificaciones inequívocas desde las notas del usuario y el tool MCP regeneró el Markdown con 381 movimientos pendientes. — EBC

* **2026-07-27** — TASK-EBC-DOC-12 cerrada: El tool export_unclassified_transactions_markdown regeneró el documento de pendientes con 410 movimientos. — EBC

* **2026-07-27** — TASK-EBC-MCP-46 cerrada: Se agregó el tool MCP export_unclassified_transactions_markdown para generar un Markdown determinista de movimientos pendientes bajo docs/. — EBC

* **2026-07-27** — TASK-EBC-DOC-11 cerrada: Se regeneró el listado de revisión con los 490 movimientos pendientes tras aplicar 49 clasificaciones confirmadas. — EBC

* **2026-07-27** — TASK-EBC-MCP-44 cerrada: Se confirmaron 49 clasificaciones respaldadas por las descripciones del usuario; la cola bajó de 539 a 490. Las transferencias internas se mantuvieron pendientes. — EBC

* **2026-07-27** — TASK-EBC-DOC-10 cerrada: Se generó el Markdown con los 539 movimientos pendientes, códigos locales y columnas para categoría y explicación manual. — EBC

* **2026-07-27** — TASK-EBC-MCP-43 cerrada: Corregida la compatibilidad Streamable HTTP con VS Code: el servidor valida la sesión cuando se recibe Mcp-Session-Id y usa la versión negociada si VS Code omite MCP-Protocol-Version. Se incorporó auditoría local segura del handshake. — EBC

* **2026-07-27** — TASK-EBC-MCP-33 cerrada: Se completó el fallback de clasificación con Azure AI: se invoca únicamente tras no encontrar una regla determinista; el prompt usa descripción normalizada y sanitizada junto con el catálogo permitido; errores y baja confianza devuelven No clasificado. Se añadieron pruebas con cliente HTTP simulado, incluida la verificación de redacción de identificadores y montos. — EBC

* **2026-07-27** — TASK-EBC-MCP-33: list_unclassified_transactions ahora pagina con page/itemsPerPage (máximo 200) y entrega el listado textual en TOON, con totalItems y totalPages. Pruebas de clasificación y paginación/TOON: 15 aprobadas. — EBC

* **2026-07-27** — TASK-EBC-MCP-42 cerrada: Corregida la validación de sesión y versión negociada para tools/list y tools/call; actualizadas las expectativas del catálogo para 11 plantillas, 41 relaciones y 9 patrones activos. ISSUE-008 resuelto. — EBC

* **2026-07-27** — ISSUE-008 resuelto: Se actualizó el contrato de pruebas a 11 plantillas, 41 relaciones y 9 patrones activos. McpHandler ahora exige sesión existente y header MCP-Protocol-Version coincidente antes de procesar métodos posteriores a initialize.

* **2026-07-27** — TASK-EBC-BE-29 cerrada: Implementado job diario BCCR de tipo de cambio USD/CRC: consulta el indicador 318 con fallback de hasta tres días, realiza upsert idempotente para BN y BAC y queda programado a las 08:00 en hora de Costa Rica. Las pruebas específicas pasan; los fallos ajenos de la suite completa se registraron en ISSUE-008. — EBC

* **2026-07-27** — TASK-EBC-MCP-33: validado flujo .NET → Azure AI → No clasificado → revisión manual. Se añadió timeout configurable de 10 s para Azure AI; un timeout queda auditado como No clasificado sin abortar el lote. Pruebas de clasificación: 14 aprobadas. — EBC

* **2026-07-27** — TASK-EBC-MCP-32 cerrada: Revisados los 6 criterios de aceptación contra el código: classify_pending_transactions ejecuta lote regla→IA→No clasificado; list_unclassified_transactions expone explicación; confirm_transaction_classification registra manual y crea/actualiza regla determinista (probado con test dedicado); TryClassifyWithAiAsync cae a No clasificado ante fallo/baja confianza sin excepción no controlada; todas las respuestas incluyen Explanation con el origen (rule/ai/manual/unclassified) sin datos bancarios sensibles. Los 9 tests de ClassificationServiceTests pasan; el único fallo al filtrar McpProtocolTests es el problema preexistente de Hangfire JobStorage en el arranque del WebApplicationFactory, no relacionado con esta tarea. — EBC
