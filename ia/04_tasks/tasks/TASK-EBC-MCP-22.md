# TASK-EBC-MCP-22 — Corregir compatibilidad MCP HTTP con VS Code y Codex

**Estado:** Lista
**Autor:** Ezequiel Baltodano Cubillo `<pendiente>`
**Rama:** `dev`
**Fecha inicio:** 2026-07-26 21:49 CR
**Fecha cierre:** —
**Área:** MCP
**Prioridad:** media
**Riesgo:** bajo
**Aprobación:** aprobada

---

## Título

Corregir compatibilidad MCP HTTP con VS Code y Codex

## Contexto

VS Code conecta a bancosMcp pero recibe HTTP 400 al enviar mensajes posteriores a initialize. El servidor solo admite dos versiones de protocolo y rechaza la versión usada por el cliente.

## Objetivo

Negociar y aceptar versiones MCP compatibles para que VS Code y Codex puedan llamar herramientas del servidor Bancos.Mcp.

## Alcance permitido

* src/Bancos.Mcp/Protocol/McpHandler.cs
* tests/Bancos.Mcp.Tests
* pruebas MCP HTTP locales

## Fuera de alcance

* Cambios de herramientas de negocio
* Cambios de base de datos
* Cambios de credenciales o despliegue

## Criterios de aceptación

* [ ] Una sesión con protocolo 2025-03-26 no recibe 400 en tools/list
* [ ] VS Code o Codex completa health_status
* [ ] Las pruebas de Bancos.Mcp pasan

## Riesgos

Riesgo bajo.

## Archivos afectados / probables

* `src/Bancos.Mcp/Protocol/McpHandler.cs`
* `tests/Bancos.Mcp.Tests`

## Plan técnico

1. Mantener sesiones por Mcp-Session-Id
2. Aceptar versiones públicas relevantes y usar la versión negociada para validar la sesión

## Pasos

1. Reproducir el 400 con versión del cliente
2. Ajustar la negociación y validación de versión
3. Agregar o adaptar prueba de protocolo
4. Verificar health_status desde un cliente MCP

## Salida esperada

El endpoint HTTP MCP acepta la versión negociada de VS Code/Codex y health_status responde en una sesión nativa.

## Validación

* [ ] dotnet test de Bancos.Mcp
* [ ] smoke HTTP initialize/tools/list/health_status

## Rollback

Revertir el cambio de compatibilidad de protocolo.

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

* Aprobada por Ezequiel Baltodano Cubillo el 2026-07-26 21:49 CR.

Sin notas adicionales.

## Issues vinculados

* ninguno
