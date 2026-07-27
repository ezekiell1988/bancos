# TASK-EBC-MCP-38 — Tools MCP de conciliación de pagos y transferencias

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

Tools MCP de conciliación de pagos y transferencias

## Contexto

Los pagos pueden conciliarse N:N y el LLM necesita proponer, consultar y confirmar relaciones sin alterar movimientos sin confirmación.

## Objetivo

Implementar tools MCP para sugerir conciliaciones y registrar correcciones manuales auditables.

## Alcance permitido

* src/Bancos.Mcp/Features/Reconciliation/
* tests/Bancos.Mcp.Tests/
* ia/

## Fuera de alcance

* Modificar archivos importados.
* Clasificar categorías con Azure AI.
* Modificar proyectos retirados.

## Criterios de aceptación

* [ ] Una tool lista partidas no conciliadas.
* [ ] Una tool propone conciliaciones N:N explicando monto, fecha y confianza.
* [ ] Una tool confirma, corrige o elimina una conciliación con auditoría.
* [ ] Las confirmaciones manuales no eliminan movimientos originales.

## Riesgos

Riesgo medio.

## Archivos afectados / probables

* `src/Bancos.Mcp/Features/Reconciliation/`
* `tests/Bancos.Mcp.Tests/`

## Plan técnico

1. Modelar relaciones de conciliación.
2. Implementar propuesta determinista.
3. Exponer tools de revisión y confirmación.
4. Probar casos N:N.

## Pasos

1. Modelar.
2. Implementar.
3. Probar.

## Salida esperada

Conciliación controlada mediante MCP.

## Validación

* [ ] dotnet test de Bancos.Mcp.
* [ ] Pruebas N:N y auditoría.

## Rollback

Revertir feature Reconciliation.

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
