# TASK-EBC-DB-11 — Modelo MCP de categorías, reglas e historial de clasificación

**Estado:** Borrador
**Autor:** Ezequiel Baltodano Cubillo `<pendiente>`
**Rama:** `dev`
**Fecha inicio:** 2026-07-26 23:46 CR
**Fecha cierre:** —
**Área:** DB
**Prioridad:** alta
**Riesgo:** medio
**Aprobación:** pendiente

---

## Título

Modelo MCP de categorías, reglas e historial de clasificación

## Contexto

Bancos.Mcp necesita clasificar movimientos sin depender siempre de IA y conservar auditoría de cada decisión.

## Objetivo

Agregar al catálogo MCP el modelo de categorías contables, reglas deterministas e historial de clasificaciones.

## Alcance permitido

* src/Bancos.Mcp/
* tests/Bancos.Mcp.Tests/
* ia/

## Fuera de alcance

* Integrar Azure AI.
* Generar reportes HTML.
* Modificar proyectos retirados.

## Criterios de aceptación

* [ ] Existen categorías jerárquicas para ingreso, gasto, activo, pasivo y capital.
* [ ] Las reglas permiten coincidencia por cuenta, descripción normalizada y contexto de movimiento.
* [ ] Cada cambio conserva origen, confianza y auditoría.
* [ ] Las migraciones solo afectan dbbancosmcp.

## Riesgos

Riesgo medio.

## Archivos afectados / probables

* `src/Bancos.Mcp/Features/Classification/`
* `src/Bancos.Mcp/Data/Migrations/`
* `tests/Bancos.Mcp.Tests/`

## Plan técnico

1. Diseñar entidades y restricciones.
2. Crear migración MCP.
3. Exponer repositorios/servicios internos deterministas.
4. Agregar pruebas de reglas y auditoría.

## Pasos

1. Modelar.
2. Migrar.
3. Probar.

## Salida esperada

Modelo persistente de clasificación determinista y auditable.

## Validación

* [ ] dotnet test de Bancos.Mcp.
* [ ] Migración aplicada a base MCP local.

## Rollback

Revertir migración y código de la feature.

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
