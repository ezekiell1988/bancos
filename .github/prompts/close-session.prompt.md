---
description: Persistir el estado de la sesión de trabajo de vuelta en /ia
---

## Leer antes de empezar

- `ia/04_tasks/current.md`
- El archivo de la tarea trabajada: `ia/04_tasks/tasks/{TASK-ID}.md`
- `ia/05_progress/current.md`
- `ia/07_issues/current.md`

## Objetivo

Actualizar `/ia` para que refleje el estado real del proyecto al finalizar la sesión.

## Reglas

- Seguir el skill `.agents/skills/ia-sesion-cierre/SKILL.md`.
- No cerrar una tarea si `Expected Output` no está cumplido.
- Si la tarea se completó: mover resumen a `04_tasks/done/{YYYY-MM}.md` y eliminar el archivo individual.
- Actualizar `05_progress/current.md` y `05_progress/by-component/{área}.md`.
- Si se identificaron issues: registrarlos en `07_issues/current.md` y crear archivo en `07_issues/open/`.
- Si se tomaron decisiones arquitectónicas: agregar ADR en `06_decisions.md`.

## Salida esperada

- `04_tasks/current.md` actualizado (estado de tarea, o tarea removida si completada).
- `05_progress/current.md` refleja el avance real.
- Nuevos issues documentados si aplica.
