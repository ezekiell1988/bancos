# TASK-EBC-MCP-34 — Tools MCP para reportes HTML de resultados y situación financiera

**Estado:** Borrador
**Autor:** Ezequiel Baltodano Cubillo `<pendiente>`
**Rama:** `dev`
**Fecha inicio:** 2026-07-26 23:47 CR
**Fecha cierre:** —
**Área:** MCP
**Prioridad:** alta
**Riesgo:** medio
**Aprobación:** pendiente

---

## Título

Tools MCP para reportes HTML de resultados y situación financiera

## Contexto

El LLM necesita solicitar reportes finales sin interfaz web, a partir de movimientos clasificados y cierres regenerados.

## Objetivo

Generar HTML autocontenido para estado de resultados por período y situación financiera.

## Alcance permitido

* src/Bancos.Mcp/Features/Reports/
* tests/Bancos.Mcp.Tests/
* ia/

## Fuera de alcance

* Crear interfaz web.
* Modificar proyectos retirados.
* Publicar reportes externamente.

## Criterios de aceptación

* [ ] Una tool devuelve HTML de ingresos versus gastos para un período.
* [ ] Una tool devuelve HTML de activos, pasivos y capital para fecha/período.
* [ ] Cada reporte incluye período, moneda, fecha de generación y advertencia de datos pendientes.
* [ ] Los totales contables son verificables mediante pruebas.

## Riesgos

Riesgo medio.

## Archivos afectados / probables

* `src/Bancos.Mcp/Features/Reports/`
* `tests/Bancos.Mcp.Tests/`

## Plan técnico

1. Definir consultas contables.
2. Renderizar HTML autocontenido.
3. Exponer tools MCP.
4. Probar totales y escape HTML.

## Pasos

1. Implementar consultas.
2. Renderizar.
3. Probar.

## Salida esperada

Dos reportes HTML obtenibles mediante MCP.

## Validación

* [ ] dotnet test de Bancos.Mcp.
* [ ] Pruebas de balance Activos = Pasivos + Capital.

## Rollback

Revertir feature Reports.

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
