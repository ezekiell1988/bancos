# TASK-EBC-BE-03 — Catálogo de cuentas auxiliares y flujo integral de importación BCR

**Estado:** Borrador
**Autor:** Ezequiel Baltodano Cubillo `<pendiente>`
**Rama:** `main`
**Fecha inicio:** 2026-07-18 13:42 CR
**Fecha cierre:** —
**Área:** BE
**Prioridad:** alta
**Riesgo:** medio
**Aprobación:** pendiente

---

## Título

Catálogo de cuentas auxiliares y flujo integral de importación BCR

## Contexto

El lector bcr-debit-csv-v1 ya detecta, valida y persiste movimientos idempotentes, pero el upload requiere un auxiliar de cuenta existente y falta una prueba HTTP/Hangfire de extremo a extremo.

## Objetivo

Permitir administrar auxiliares de cuenta y comprobar una importación BCR completa usando solamente fixtures anonimizados.

## Alcance permitido

* Crear endpoints mínimos para propietarios, cuentas y auxiliares requeridos por importación.
* Agregar prueba de integración upload a job y movimientos persistidos.
* Usar exclusivamente fixtures anonimizados.

## Fuera de alcance

* Cargar documentos financieros reales.
* Construir interfaz Angular.
* Clasificación automática.

## Criterios de aceptación

* [ ] Se puede crear o consultar el auxiliar requerido por upload.
* [ ] Un fixture BCR anonimizado completa upload, job y persistencia.
* [ ] Reintentos no duplican movimientos.
* [ ] Errores de validación quedan auditados.

## Riesgos

Riesgo medio.

## Archivos afectados / probables

* `src/Bancos.Api/Features/Accounts`
* `src/Bancos.Api/Features/Imports`
* `tests/Bancos.Api.Tests`

## Plan técnico

1. Agregar API mínima del catálogo contable.
2. Preparar fixture BCR anonimizado.
3. Ejecutar job con infraestructura de prueba.
4. Verificar idempotencia y auditoría.

## Pasos

1. Implementar catálogo.
2. Agregar pruebas integrales.
3. Validar build y pruebas.

## Salida esperada

Flujo BCR verificable de extremo a extremo.

## Validación

* [ ] dotnet test
* [ ] dotnet build
* [ ] Prueba de integración de upload y job

## Rollback

Deshabilitar los endpoints nuevos sin borrar registros de auditoría.

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

Trabajar directamente en main durante construcción.

## Issues vinculados

* ninguno
