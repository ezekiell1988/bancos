# TASK-EBC-MCP-17 — Revisar implementación MCP — Estado de cuenta de tarjeta Banco Nacional

**Estado:** Borrador
**Autor:** Ezequiel Baltodano Cubillo `<pendiente>`
**Rama:** `dev`
**Fecha inicio:** 2026-07-22 18:00 CR
**Fecha cierre:** —
**Área:** MCP
**Prioridad:** media
**Riesgo:** bajo
**Aprobación:** pendiente

---

## Título

Revisar implementación MCP — Estado de cuenta de tarjeta Banco Nacional

## Contexto

Auditoría técnica de la plantilla bn-card-statement-pdf-v1 (pdf) y su parser bn-card-statement-pdf.

## Objetivo

Verificar que la detección, el parser y el flujo de importación de la plantilla Estado de cuenta de tarjeta Banco Nacional funcionen de forma consistente, registrando hallazgos sin modificar la implementación.

## Alcance permitido

* Revisar detección y asignación de parser.
* Revisar parser, validaciones y job relacionado.
* Ejecutar validaciones seguras sin alterar datos.
* Documentar hallazgos y abrir incidencias si corresponde.

## Fuera de alcance

* Modificar parsers, migraciones, configuraciones o datos de negocio.
* Cambios en producción o cargas no destinadas a pruebas.

## Criterios de aceptación

* [ ] Se coteja detección contra plantilla y parser esperados.
* [ ] Se revisan validaciones, errores e idempotencia.
* [ ] Se verifica persistencia esperada o se documenta la limitación.
* [ ] Hallazgos y acciones recomendadas registrados.

## Riesgos

Riesgo bajo.

## Archivos afectados / probables

* `pendiente de confirmar`

## Plan técnico

1. Inspección focalizada de código y configuración.
2. Pruebas y consultas de solo lectura cuando apliquen.

## Pasos

1. Localizar configuración y código del parser.
2. Revisar recorrido detección → job → persistencia.
3. Ejecutar validaciones no destructivas relevantes.
4. Registrar resultado y crear incidencia si procede.

## Salida esperada

Informe de revisión de bn-card-statement-pdf-v1, con hallazgos, riesgos y acciones recomendadas.

## Validación

* [ ] Compilación o pruebas focalizadas disponibles.
* [ ] Validación de detección por MCP con insumo seguro.
* [ ] Revisión de logs y persistencia solo lectura cuando aplique.

## Rollback

No se realizan cambios de código ni de datos.

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
