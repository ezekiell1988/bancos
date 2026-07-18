---
description: Revisar el diff activo antes de commit o pull request
---

## Leer antes de empezar

- `ia/00_context.md`
- `ia/02_architecture.md`
- `ia/06_decisions.md`
- El archivo de la tarea relacionada: `ia/04_tasks/tasks/{TASK-ID}.md`

## Objetivo

Revisar el cambio actual (`git diff`) verificando corrección, seguridad y adherencia a las decisiones del proyecto.

## Reglas

- Verificar que el `Expected Output` de la tarea se cumple.
- Detectar: bugs de lógica, vulnerabilidades OWASP top 10, código innecesario fuera del scope de la tarea.
- No proponer refactorizaciones no relacionadas con la tarea.
- Si se identifican ADRs implícitos, sugerir registrarlos en `06_decisions.md`.

## Salida esperada

- Lista de hallazgos agrupados por severidad (bloqueante / importante / sugerencia).
- Confirmación de que el diff cumple el `Expected Output` de la tarea.
