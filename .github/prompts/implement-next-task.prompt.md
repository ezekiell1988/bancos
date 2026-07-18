---
description: Seleccionar e implementar la próxima tarea de la cola activa
---

## Leer antes de empezar

- `ia/00_context.md`
- `ia/04_tasks/current.md`
- `ia/04_tasks/blocked.md`
- `ia/07_issues/current.md`
- El archivo específico de la tarea seleccionada: `ia/04_tasks/tasks/{TASK-ID}.md`

## Objetivo

Implementar la tarea con mayor prioridad disponible (no bloqueada) en `ia/04_tasks/current.md`.

## Reglas

- Cargar el skill del área técnica antes de codificar (ver mapa área→skill en `ia/04_tasks.md`).
- No implementar más de lo que define `Steps` y `Expected Output` de la tarea.
- Si se encuentra un bloqueador, documentarlo en `ia/04_tasks/blocked.md` y detenerse.
- Al terminar, usar el skill `ia-sesion-cierre` para actualizar `/ia`.

## Salida esperada

- Código implementado según `Steps` y verificable con `Expected Output`.
- `/ia` actualizado al cerrar la sesión.
