# TASK-EBC-FE-03 — Diseño de vista de préstamos: definir con el usuario qué información analizar y cómo presentarla

**Estado:** Borrador
**Autor:** ezekiell1988 `<ezekiell1988@hotmail.com>`
**Rama:** `dev`
**Fecha inicio:** 2026-07-20 20:35 CR
**Fecha cierre:** —
**Área:** FE
**Prioridad:** media
**Riesgo:** bajo
**Aprobación:** pendiente

---

## Título

Diseño de vista de préstamos: definir con el usuario qué información analizar y cómo presentarla

## Contexto

El usuario quiere definir cómo se debe presentar la información de préstamos. Aún no está claro qué métricas o vistas son más útiles para el análisis. Esta tarea es un ejercicio de descubrimiento de requisitos y diseño de UI/reporte, a completar en sesión interactiva con el usuario.

## Objetivo

Acordar con el usuario qué datos de préstamos mostrar (saldo capital, intereses pagados, cuotas pendientes, tabla de amortización, diferencial cambiario en préstamos USD, etc.) y en qué formato presentarlos.

## Alcance permitido

* Sesión de requisitos con el usuario
* Mockup o borrador de vista de préstamos
* Definición de métricas clave a mostrar

## Fuera de alcance

* Implementación final de la vista (depende del resultado de esta tarea)
* Cuentas de débito y tarjetas de crédito

## Criterios de aceptación

* [ ] El usuario aprueba el diseño de la vista de préstamos
* [ ] Existe un documento con la estructura acordada
* [ ] Se crea tarea de implementación con los requisitos definidos

## Riesgos

Riesgo bajo.

## Archivos afectados / probables

* `pendiente de confirmar`

## Plan técnico

1. Presentar al usuario los datos de préstamos disponibles en DB
2. Proponer opciones de visualización: tabla de amortización, resumen por préstamo, vista consolidada
3. Recoger preferencias y acordar estructura final
4. Documentar la decisión en un ADR o nota de requisitos

## Pasos

1. 1. Consultar préstamos activos en DB y listar campos disponibles
2. 2. Proponer 2-3 opciones de vista al usuario
3. 3. El usuario elige y ajusta
4. 4. Documentar el diseño acordado
5. 5. Crear tarea de implementación derivada

## Salida esperada

Diseño aprobado de la vista de préstamos y tarea de implementación creada.

## Validación

* [ ] El usuario dice explícitamente que el diseño es correcto
* [ ] La tarea de implementación está creada en estado Borrador

## Rollback

No aplica: es discovery y documentación.

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
