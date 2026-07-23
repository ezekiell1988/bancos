# ISSUE-005 — MCP protocol tests fail when Hangfire is not configured in Testing

**Severidad:** medium
**Estado:** abierto
**Componente:** MCP / Tests
**Detectado:** 2026-07-22 17:53 CR
**Autor:** Ezequiel Baltodano Cubillo `<pendiente>`

---

## Síntoma

McpProtocolTests retorna 500 porque ProcessImportFileTool requiere IBackgroundJobClient, que no se registra cuando Testing no tiene cadena de conexión.

## Causa raíz

AddFileProcessingModule registra ProcessImportFileTool incluso cuando no registra Hangfire.

## Workaround

ninguno

## Fix propuesto

Registrar la herramienta de proceso solo con Hangfire configurado o suministrar un cliente de jobs de prueba.

## Tareas vinculadas

* TASK-EBC-BE-26
