# TASK-EBC-MCP-37 — Tools MCP para tipos de cambio y resolución de tasas

**Estado:** Borrador
**Autor:** Ezequiel Baltodano Cubillo `<pendiente>`
**Rama:** `dev`
**Fecha inicio:** 2026-07-26 23:51 CR
**Fecha cierre:** —
**Área:** MCP
**Prioridad:** alta
**Riesgo:** medio
**Aprobación:** pendiente

---

## Título

Tools MCP para tipos de cambio y resolución de tasas

## Contexto

El LLM debe consultar y registrar el tipo de cambio diario necesario para importaciones, cierres y reportes.

## Objetivo

Exponer tools MCP para listar, registrar y resolver tipos de cambio con el fallback acordado.

## Alcance permitido

* src/Bancos.Mcp/Features/ExchangeRates/
* tests/Bancos.Mcp.Tests/
* ia/

## Fuera de alcance

* Descarga automática desde fuentes externas.
* Modificar proyectos retirados.

## Criterios de aceptación

* [ ] Una tool consulta tasas por fecha y moneda.
* [ ] Una tool registra o corrige una tasa manual con auditoría.
* [ ] Una tool resuelve la tasa aplicable usando el último valor previo cuando falte la fecha exacta.
* [ ] La tool informa cuando se necesita intervención humana.

## Riesgos

Riesgo medio.

## Archivos afectados / probables

* `src/Bancos.Mcp/Features/ExchangeRates/`
* `tests/Bancos.Mcp.Tests/`

## Plan técnico

1. Definir contratos y validaciones monetarias.
2. Implementar resolución de fallback.
3. Probar fecha exacta, fallback y ausencia total.

## Pasos

1. Diseñar.
2. Implementar.
3. Probar.

## Salida esperada

Gestión auditable de tasas disponible mediante MCP.

## Validación

* [ ] dotnet test de Bancos.Mcp.
* [ ] Pruebas de fallback.

## Rollback

Revertir feature ExchangeRates.

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

Sin notas adicionales.

## Issues vinculados

* ninguno
