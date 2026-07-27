# TASK-EBC-MCP-32 — Tools MCP de clasificación determinista y revisión manual aprendible

**Estado:** Lista
**Autor:** Ezequiel Baltodano Cubillo `<pendiente>`
**Rama:** `dev`
**Fecha inicio:** 2026-07-26 23:47 CR
**Fecha cierre:** —
**Área:** MCP
**Prioridad:** alta
**Riesgo:** medio
**Aprobación:** aprobada

---

## Título

Tools MCP de clasificación determinista y revisión manual aprendible

## Contexto

El LLM debe poder consultar movimientos pendientes, aplicar la clasificación y convertir correcciones del usuario en reglas reutilizables.

## Objetivo

Exponer tools MCP para clasificar movimientos con reglas .NET, revisar pendientes y aprender correcciones manuales.

## Alcance permitido

* src/Bancos.Mcp/Features/Classification/
* tests/Bancos.Mcp.Tests/
* ia/

## Fuera de alcance

* Llamadas a Azure AI.
* Cambios a proyectos retirados.

## Criterios de aceptación

* [ ] Una tool ejecuta clasificación determinista por lote.
* [ ] Una tool lista No clasificados con explicación.
* [ ] Una tool registra clasificación confirmada y crea/actualiza regla determinista.
* [ ] Las respuestas explican origen de la decisión sin datos sensibles innecesarios.

## Riesgos

Riesgo medio.

## Archivos afectados / probables

* `src/Bancos.Mcp/Features/Classification/`
* `tests/Bancos.Mcp.Tests/`

## Plan técnico

1. Definir contratos MCP.
2. Implementar flujo y validaciones.
3. Probar idempotencia y aprendizaje manual.

## Pasos

1. Diseñar tools.
2. Implementar.
3. Probar.

## Salida esperada

Clasificación y aprendizaje manual disponibles mediante MCP.

## Validación

* [ ] dotnet test de Bancos.Mcp.
* [ ] Pruebas de tool con movimiento ya conocido y pendiente.

## Rollback

Revertir cambios de feature y contratos MCP.

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

* Aprobada por Ezequiel Baltodano Cubillo el 2026-07-27 08:22 CR.

Sin notas adicionales.

## Issues vinculados

* ninguno
