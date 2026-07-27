# TASK-EBC-MCP-35 — Tools MCP de consulta de catálogo, períodos y movimientos

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

Tools MCP de consulta de catálogo, períodos y movimientos

## Contexto

El LLM necesita consultar cuentas, períodos y movimientos persistidos sin acceder directamente a la base de datos.

## Objetivo

Exponer tools MCP de solo lectura para catálogo bancario, períodos y movimientos con filtros seguros y resultados paginados.

## Alcance permitido

* src/Bancos.Mcp/Features/
* tests/Bancos.Mcp.Tests/
* ia/

## Fuera de alcance

* Modificar datos financieros.
* Usar dbquery como interfaz de producto.
* Modificar proyectos retirados.

## Criterios de aceptación

* [ ] Existen tools para listar cuentas y períodos.
* [ ] Existe una tool para buscar movimientos por período, cuenta, estado de clasificación y rango de fechas.
* [ ] Existe una tool de detalle de movimiento con clasificación y trazabilidad.
* [ ] Las respuestas son paginadas y no exponen credenciales ni archivos fuente.

## Riesgos

Riesgo medio.

## Archivos afectados / probables

* `src/Bancos.Mcp/Features/Accounts/`
* `src/Bancos.Mcp/Features/Transactions/`
* `tests/Bancos.Mcp.Tests/`

## Plan técnico

1. Definir contratos de consulta MCP.
2. Implementar filtros, orden estable y paginación.
3. Agregar pruebas de autorización de filtros y serialización.

## Pasos

1. Diseñar.
2. Implementar.
3. Probar.

## Salida esperada

Tools MCP de consulta para que el LLM pueda decidir acciones sobre datos persistidos.

## Validación

* [ ] dotnet test de Bancos.Mcp.
* [ ] Pruebas de filtros y paginación.

## Rollback

Revertir las tools y servicios de consulta.

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
