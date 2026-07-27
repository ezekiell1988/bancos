# TASK-EBC-MCP-23 — Excluir sondeos MCP del rate limit global

**Estado:** Lista
**Autor:** Ezequiel Baltodano Cubillo `<pendiente>`
**Rama:** `dev`
**Fecha inicio:** 2026-07-26 21:59 CR
**Fecha cierre:** —
**Área:** MCP
**Prioridad:** alta
**Riesgo:** bajo
**Aprobación:** aprobada

---

## Título

Excluir sondeos MCP del rate limit global

## Contexto

VS Code recibe HTTP 429 al sondear y al intentar fallback SSE porque el limitador global consume cuota para GET y DELETE /mcp.

## Objetivo

Mantener el límite de POST MCP sin limitar GET/DELETE de establecimiento y cierre de sesión.

## Alcance permitido

* src/Bancos.Mcp/Features/TemplateDetection/TemplateDetectionModule.cs
* tests/Bancos.Mcp.Tests
* pruebas HTTP locales

## Fuera de alcance

* Cambios de herramientas de negocio
* Cambios de base de datos
* Cambios de autenticación

## Criterios de aceptación

* [ ] GET /mcp no recibe 429 por sondeos
* [ ] POST /mcp permanece limitado
* [ ] health_status funciona

## Riesgos

Riesgo bajo.

## Archivos afectados / probables

* `src/Bancos.Mcp/Features/TemplateDetection/TemplateDetectionModule.cs`
* `tests/Bancos.Mcp.Tests/McpProtocolTests.cs`

## Plan técnico

1. Devolver NoLimiter para GET/DELETE /mcp
2. Conservar fixed-window para POST y la política de concurrencia existente

## Pasos

1. Restringir el limitador global a POST /mcp
2. Verificar GET repetidos sin 429
3. Verificar POST MCP mantiene respuesta correcta

## Salida esperada

VS Code puede sondear, conectar y cerrar bancosMcp sin HTTP 429 causado por el limitador global.

## Validación

* [ ] dotnet build
* [ ] smoke HTTP local

## Rollback

Restaurar la partición global anterior.

## Dependencias

* ninguna

## Checklist

* [ ] Alcance revisado
* [ ] Riesgo revisado
* [ ] Aprobación registrada si aplica
* [ ] Implementación completa
* [ ] Validación completa
* [ ] Progreso/documentación actualizado

## Notas / contexto adicional

* Aprobada por Ezequiel Baltodano Cubillo el 2026-07-26 21:59 CR.

Sin notas adicionales.

## Issues vinculados

* ninguno
