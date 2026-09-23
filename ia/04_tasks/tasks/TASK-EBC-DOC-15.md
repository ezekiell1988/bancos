# TASK-EBC-DOC-15 — Definir el mapa de área a skill del proyecto

**schemaVersion:** 2
**Estado:** Borrador
**DevOps:** —
**CRM:** —
**Autor:** Ezequiel Baltodano Cubillo `<ezekiell1988@hotmail.com>`
**Aprobado por:** —
**Rama:** —
**createdAt:** 2026-09-23T12:29:52.782-06:00
**approvedAt:** —
**startedAt:** —
**finishedAt:** —
**Área:** DOC
**Prioridad:** media
**Riesgo:** bajo
**Aprobación:** pendiente
**parentId:** —
**workItemType:** Task
**workstream:** ia-workflow
**tags:** doc, ia-workflow

---

## Título

Definir el mapa de área a skill del proyecto

## Contexto

El skill global `project-task-management` carga las reglas propias del proyecto desde `ia/rules/mapa-area-skill.md`. El archivo existe como plantilla con un bloque POR DEFINIR porque requiere conocer el stack, los ADRs y las convenciones reales del proyecto.

## Objetivo

Dejar `ia/rules/mapa-area-skill.md` sin marcadores, con contenido concreto y verificable.

## Alcance permitido

* Completar `ia/rules/mapa-area-skill.md` con reglas reales del proyecto.

## Fuera de alcance

* Modificar los skills globales `project-*`, compartidos por todos los proyectos.
* Reescribir la estructura de `/ia`.

## Criterios de aceptación

* `ia/rules/mapa-area-skill.md` no contiene `{…}` ni el bloque POR DEFINIR.
* Toda área usada por las tareas tiene al menos un skill asignado o una nota de que no aplica.

## Archivos afectados / probables

* No definidos.

## Plan técnico

* Leer contexto, arquitectura y ADRs del proyecto.
* Redactar el contenido y reemplazar la plantilla.

## Pasos

1. Leer el contexto del proyecto.
2. Completar el archivo.
3. Verificar que no quedan marcadores.

## Salida esperada

Un `mapa-area-skill.md` operativo con reglas propias del proyecto.

## Validación

* grep -n "POR DEFINIR" ia/rules/mapa-area-skill.md no devuelve resultados.

## Reversión

Revertir el archivo vía Git.

## Dependencias

* Ninguna.

## Notas

—

## Issues vinculados

* Ninguno.

## Historial de eventos V2

```jsonl
{"schemaVersion":2,"eventId":"f7ac2d51-03ea-4f58-913a-fe8e3c284f54","taskId":"TASK-EBC-DOC-15","event":"created","occurredAt":"2026-09-23T12:29:52.782-06:00","actor":"MCP iaWorkflow","reason":"Tarea creada en Borrador."}
```
