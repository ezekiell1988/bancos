# TASK-EBC-BE-19 — Corregir fallos restantes de lectores de importación

**Estado:** Lista
**Autor:** Ezequiel Baltodano Cubillo `<pendiente>`
**Rama:** `dev`
**Fecha inicio:** 2026-07-18 18:47 CR
**Fecha cierre:** —
**Área:** BE
**Prioridad:** alta
**Riesgo:** medio
**Aprobación:** aprobada

---

## Título

Corregir fallos restantes de lectores de importación

## Contexto

Tras aplicar la migración OperationType, únicamente el job 8 completó. Los demás jobs existentes fallan durante el procesamiento de archivos bancarios.

## Objetivo

Diagnosticar y corregir las causas comunes de los jobs de importación fallidos, conservando el archivo y dejando cada job en estado final sin reintentos.

## Alcance permitido

* Lectores y detectores de plantillas de importación
* Manejo de errores de parsing y pruebas asociadas
* Consulta de estados y errores mediante dbquery

## Fuera de alcance

* Cambios de credenciales o proveedor IA
* Despliegue productivo
* Exposición de datos financieros

## Criterios de aceptación

* [ ] Los fallos se agrupan por causa usando los jobs existentes.
* [ ] Se corrigen las causas reproducibles en código.
* [ ] Los jobs procesados no usan reintentos.
* [ ] Las pruebas automatizadas relevantes pasan.

## Riesgos

Riesgo medio.

## Archivos afectados / probables

* `src/Bancos.Api/Features/Imports`
* `tests/Bancos.Api.Tests`

## Plan técnico

1. Consultar estados e historial de jobs con dbquery.
2. Reproducir cada familia de error con pruebas seguras.
3. Ajustar detectores/lectores y mensajes de validación.
4. Compilar, ejecutar pruebas y verificar resultado.

## Pasos

1. Inspeccionar jobs fallidos.
2. Implementar correcciones.
3. Validar y reiniciar el entorno si corresponde.

## Salida esperada

Lectores robustos para los formatos cargados y causas de error corregidas o convertidas en mensajes accionables.

## Validación

* [ ] dotnet test
* [ ] Consulta read-only a estado de jobs
* [ ] Prueba de importación en entorno local

## Rollback

Revertir únicamente los cambios de lectores y reejecutar trabajos con los archivos persistidos.

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

* Aprobada por Ezequiel Baltodano Cubillo el 2026-07-18 18:47 CR.

Sin notas adicionales.

## Issues vinculados

* ninguno
