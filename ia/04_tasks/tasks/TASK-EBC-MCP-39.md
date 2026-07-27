# TASK-EBC-MCP-39 — Tools MCP de libro mayor, regeneración y diferencial cambiario

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

Tools MCP de libro mayor, regeneración y diferencial cambiario

## Contexto

Después de importaciones, clasificación y tasas, el LLM debe regenerar períodos y obtener comprobantes sin ejecutar SQL directo.

## Objetivo

Exponer tools MCP para consultar libro mayor, regenerar períodos afectados y calcular diferencial cambiario de pasivos USD.

## Alcance permitido

* src/Bancos.Mcp/Features/Ledger/
* src/Bancos.Mcp/Features/ForeignExchange/
* tests/Bancos.Mcp.Tests/
* ia/

## Fuera de alcance

* Diferencial cambiario de activos USD.
* Modificar proyectos retirados.

## Criterios de aceptación

* [ ] Una tool consulta comprobantes y líneas por período.
* [ ] Una tool encola regeneración desde el período afectado con identificador de job.
* [ ] Una tool calcula o regenera el cierre cambiario de pasivos USD.
* [ ] Los resultados indican estado, período afectado y advertencias de datos faltantes.

## Riesgos

Riesgo medio.

## Archivos afectados / probables

* `src/Bancos.Mcp/Features/Ledger/`
* `src/Bancos.Mcp/Features/ForeignExchange/`
* `tests/Bancos.Mcp.Tests/`

## Plan técnico

1. Definir servicios de regeneración.
2. Registrar jobs sin bytes.
3. Exponer consulta de comprobantes y cierre FX.
4. Probar regeneración idempotente.

## Pasos

1. Implementar.
2. Probar.

## Salida esperada

Contabilidad y cierre FX operables mediante MCP.

## Validación

* [ ] dotnet test de Bancos.Mcp.
* [ ] Pruebas de cierre regenerable.

## Rollback

Revertir features Ledger y ForeignExchange.

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
