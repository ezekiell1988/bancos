---
title: IA Workflow Code Review Skill Reference
description: Referencia y checklist para crear el skill de revisión de código del proyecto que valida diffs contra las reglas de /ia.
---

## Propósito

El skill de revisión de código revisa un conjunto de cambios contra las reglas, decisiones y problemas conocidos registrados en `/ia` antes del commit o pull request. Es el segundo de los tres skills de workflow que operan sobre `/ia`.

Crear este skill para que cada cambio sea verificado contra el mismo contrato del proyecto.

## Cuándo leer

* Al inicializar el workflow `/ia` en un proyecto nuevo.
* Al auditar si existe un checklist de revisión que refleje los ADRs actuales.
* Al adaptar un skill de revisión de código existente tras cambios de arquitectura o de reglas.

## Pertenece a

* El checklist de revisión específico del proyecto.
* Clasificación de hallazgos (bloqueante, riesgo, mejora) y el veredicto final.
* Detección de exposición de secretos en diffs.

## No pertenece a

* Escribir la corrección. La revisión reporta hallazgos; no implementa salvo que se solicite.
* Ciclo de vida de tareas. Eso pertenece al skill de gestión de tareas.
* Registrar decisiones. Los ADRs viven en `06_decisions/`.

## Frontmatter recomendado del skill

```yaml
---
name: project-code-review
description: Review project diffs against business rules, ADRs, mandatory patterns and secret exposure.
---
```

## Cuerpo recomendado del skill

* `Propósito`: revisar cambios contra las reglas del proyecto antes del commit o PR.
* `Cuándo usar`: una tarea está completa, el usuario pide una revisión, o corre un agente de QA.
* `Contexto requerido`: `02_architecture.md`, `06_decisions.md` como índice de ADRs, solo los archivos relevantes bajo `06_decisions/`, `07_issues.md` y el archivo de la tarea de origen.
* `Procedimiento`: obtener el diff, verificar la Salida esperada de la tarea, aplicar el checklist, clasificar hallazgos, emitir un veredicto.
* `Reglas de seguridad`: no corregir código silenciosamente; nunca citar valores de secretos, reportar solo la ubicación.
* `Salida esperada`: un reporte con bloqueantes, riesgos, mejoras y un veredicto final.

## Construir el checklist

El checklist es el núcleo específico del proyecto. Derivar items desde `/ia`:

* Reglas de framework: detección de cambios, flujo de control, patrones obligatorios de UI.
* Reglas de backend: cero advertencias, límites de capas, nullabilidad.
* Reglas de datos: scripts trazables, paginación segura, filtros indexados.
* Secretos: sin tokens, contraseñas, cadenas de conexión ni claves en el diff.
* Decisiones: el cambio no contradice un ADR vigente.
* Documentación: `/ia` actualizado cuando la tarea lo requiere.

Mantener las reglas concretas del proyecto en este skill, no en `ia-workflow`.

## Errores comunes

* Un checklist que se desfa de los ADRs actuales indexados en `06_decisions.md`.
* Citar un valor de secreto en el reporte en vez de apuntar a su ubicación.
* Revisar sin leer la tarea de origen, haciendo que se pierda la expansión de alcance.
