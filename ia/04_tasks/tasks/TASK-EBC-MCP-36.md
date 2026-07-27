# TASK-EBC-MCP-36 — Tools MCP de idempotencia y seguimiento de importaciones

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

Tools MCP de idempotencia y seguimiento de importaciones

## Contexto

process_import_file encola procesamiento, pero el LLM necesita conocer el resultado, detectar duplicados y reintentar fallos de manera controlada.

## Objetivo

Completar la operación de importación con tools para consultar estado, huellas, errores y reintentos seguros.

## Alcance permitido

* src/Bancos.Mcp/Features/FileProcessing/
* src/Bancos.Mcp/Features/Imports/
* tests/Bancos.Mcp.Tests/
* ia/

## Fuera de alcance

* Cambiar parsers no relacionados.
* Enviar bytes de archivos a argumentos Hangfire.
* Modificar proyectos retirados.

## Criterios de aceptación

* [ ] Una tool consulta el estado y detalle de una importación o job.
* [ ] Una tool lista importaciones recientes y duplicados por huella.
* [ ] Un reintento solo se permite para fallos idempotentes y usa identificadores, no bytes.
* [ ] Las respuestas explican el siguiente paso y no devuelven contenido de archivos.

## Riesgos

Riesgo medio.

## Archivos afectados / probables

* `src/Bancos.Mcp/Features/FileProcessing/`
* `src/Bancos.Mcp/Features/Imports/`
* `tests/Bancos.Mcp.Tests/`

## Plan técnico

1. Persistir/consultar estado de procesamiento.
2. Definir tools de seguimiento y reintento.
3. Probar duplicados, fallos y reintentos.

## Pasos

1. Modelar estado.
2. Implementar.
3. Probar.

## Salida esperada

Ciclo de importación observable e idempotente mediante MCP.

## Validación

* [ ] dotnet test de Bancos.Mcp.
* [ ] Pruebas de huella duplicada y reintento.

## Rollback

Revertir tools y servicios de seguimiento.

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
