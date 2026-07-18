# TASK-EBC-BE-06 — Clasificación determinística de movimientos

**Estado:** Borrador
**Autor:** Ezequiel Baltodano Cubillo `<pendiente>`
**Rama:** `main`
**Fecha inicio:** 2026-07-18 13:42 CR
**Fecha cierre:** —
**Área:** BE
**Prioridad:** media
**Riesgo:** medio
**Aprobación:** pendiente

---

## Título

Clasificación determinística de movimientos

## Contexto

Los movimientos importados requieren clasificación antes de usar IA; la arquitectura ya define prioridad por coincidencia exacta, reglas por patrón, categoría previa y General.

## Objetivo

Clasificar movimientos mediante reglas determinísticas auditables y revisión manual pendiente.

## Alcance permitido

* Implementar servicio de clasificación por cuenta y descripción normalizada.
* Aplicar reglas por patrón y categoría previa.
* Asignar General cuando no exista coincidencia.
* Exponer endpoints mínimos de reglas y revisión.

## Fuera de alcance

* Azure AI.
* Dashboard Angular.
* Reclasificación masiva no revisada.

## Criterios de aceptación

* [ ] Coincidencia exacta aprobada prevalece.
* [ ] Las reglas por patrón se aplican de forma determinística.
* [ ] Sin coincidencia asigna General y revisión pendiente.
* [ ] Cambios quedan auditados.

## Riesgos

Riesgo medio.

## Archivos afectados / probables

* `src/Bancos.Api/Features/Classification`
* `src/Bancos.Api/Features/Imports`
* `tests/Bancos.Api.Tests`

## Plan técnico

1. Completar modelo de categorías y reglas.
2. Implementar servicio y orden de precedencia.
3. Integrar al cierre de importación.
4. Agregar pruebas.

## Pasos

1. Modelar revisión.
2. Implementar servicio.
3. Conectar importación.
4. Validar.

## Salida esperada

Clasificación local segura sin IA.

## Validación

* [ ] dotnet test
* [ ] dotnet build
* [ ] Pruebas de precedencia de reglas

## Rollback

Deshabilitar la ejecución automática y conservar clasificaciones auditadas.

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
