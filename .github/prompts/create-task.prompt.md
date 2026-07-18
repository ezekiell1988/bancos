---
description: Convertir un requisito en una tarea accionable en /ia/04_tasks
---

## Leer antes de empezar

- `ia/00_context.md`
- `ia/04_tasks.md`
- `ia/04_tasks/current.md`
- `ia/04_tasks/backlog.md`
- `ia/templates/task-template.md`

## Objetivo

Crear un archivo de tarea en `ia/04_tasks/tasks/{TASK-ID}.md` y registrar la tarea en `ia/04_tasks/current.md` o `ia/04_tasks/backlog.md` según su estado.

## Reglas

- Seguir el skill `.agents/skills/ia-nueva-tarea/SKILL.md`.
- Asignar el siguiente ID secuencial disponible para la persona y área.
- `Expected Output` debe ser verificable — si no se puede verificar, redefinir la tarea.
- No mezclar múltiples features en una sola tarea.

## Salida esperada

- Archivo `ia/04_tasks/tasks/{TASK-ID}.md` creado desde el template.
- Fila agregada en `ia/04_tasks/current.md` (o `backlog.md` si no está lista).
