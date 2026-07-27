# TASK-EBC-MCP-40 — Tools MCP de consulta para cortes de tarjeta, financiamientos y préstamos

**Estado:** Borrador
**Autor:** Ezequiel Baltodano Cubillo `<pendiente>`
**Rama:** `dev`
**Fecha inicio:** 2026-07-26 23:51 CR
**Fecha cierre:** —
**Área:** MCP
**Prioridad:** media
**Riesgo:** medio
**Aprobación:** pendiente

---

## Título

Tools MCP de consulta para cortes de tarjeta, financiamientos y préstamos

## Contexto

Los extractos ya se importan a tablas transaccionales, pero el LLM necesita consultarlos para análisis, pagos y reportes.

## Objetivo

Exponer tools MCP de solo lectura para cortes de tarjeta, sus movimientos vinculados, financiamientos y calendarios de préstamos.

## Alcance permitido

* src/Bancos.Mcp/Features/CardStatements/
* src/Bancos.Mcp/Features/Loans/
* tests/Bancos.Mcp.Tests/
* ia/

## Fuera de alcance

* Modificar extractos importados.
* Crear interfaz web.
* Modificar proyectos retirados.

## Criterios de aceptación

* [ ] Una tool consulta cortes y líneas asociadas por cuenta/período.
* [ ] Una tool lista financiamientos de tarjeta activos.
* [ ] Una tool consulta extractos y cuotas de préstamos.
* [ ] Las respuestas incluyen saldos y fechas necesarias para análisis, sin exponer archivos fuente.

## Riesgos

Riesgo medio.

## Archivos afectados / probables

* `src/Bancos.Mcp/Features/CardStatements/`
* `src/Bancos.Mcp/Features/Loans/`
* `tests/Bancos.Mcp.Tests/`

## Plan técnico

1. Definir DTOs y filtros.
2. Implementar tools de consulta paginada.
3. Probar vínculos movimiento-corte y cuotas.

## Pasos

1. Implementar.
2. Probar.

## Salida esperada

Datos de tarjetas y préstamos disponibles para el LLM mediante MCP.

## Validación

* [ ] dotnet test de Bancos.Mcp.
* [ ] Pruebas de filtros y relaciones.

## Rollback

Revertir tools de consulta.

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
