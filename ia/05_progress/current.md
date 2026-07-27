# Progreso actual

> **Última actualización:** 2026-07-27 CR (TASK-EBC-BE-29 completada)

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



* **2026-07-27** — TASK-EBC-BE-29 cerrada: Implementado job diario BCCR de tipo de cambio USD/CRC: consulta el indicador 318 con fallback de hasta tres días, realiza upsert idempotente para BN y BAC y queda programado a las 08:00 en hora de Costa Rica. Las pruebas específicas pasan; los fallos ajenos de la suite completa se registraron en ISSUE-008. — EBC

* **2026-07-27** — TASK-EBC-MCP-33: validado flujo .NET → Azure AI → No clasificado → revisión manual. Se añadió timeout configurable de 10 s para Azure AI; un timeout queda auditado como No clasificado sin abortar el lote. Pruebas de clasificación: 14 aprobadas. — EBC

* **2026-07-27** — TASK-EBC-MCP-32 cerrada: Revisados los 6 criterios de aceptación contra el código: classify_pending_transactions ejecuta lote regla→IA→No clasificado; list_unclassified_transactions expone explicación; confirm_transaction_classification registra manual y crea/actualiza regla determinista (probado con test dedicado); TryClassifyWithAiAsync cae a No clasificado ante fallo/baja confianza sin excepción no controlada; todas las respuestas incluyen Explanation con el origen (rule/ai/manual/unclassified) sin datos bancarios sensibles. Los 9 tests de ClassificationServiceTests pasan; el único fallo al filtrar McpProtocolTests es el problema preexistente de Hangfire JobStorage en el arranque del WebApplicationFactory, no relacionado con esta tarea. — EBC
