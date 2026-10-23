---
description: Cerrar la sesión de trabajo con la regla ia/rules/cierre-sesion.md.
---

## Leer

* Con el MCP `iaWorkflow`: `ia_validate` e `ia_get_context` con `intent: "cerrar_sesion"`.
* Sin MCP (fallback): `ia/04_tasks/current.md`, `ia/05_progress/current.md`.
* Regla del proyecto: `rules/cierre-sesion.md` (con el MCP, `ia_inspect` `action: "read_file"`; sin MCP, `ia/rules/`).

## Objetivo

Dejar `/ia` consistente al final de la sesión: tareas, progreso, issues y decisiones sincronizados.

## Reglas

* Seguir la secuencia de cierre completa de `ia/rules/cierre-sesion.md`.
* No editar manualmente archivos o índices de `/ia` si el MCP `iaWorkflow` está disponible: `finish_task` e `ia_add_progress_entry` mantienen el plan y las partes gestionadas de `05_progress/current.md`.
* A mano solo `Resumen ejecutivo`, `Pendiente` y `Riesgos y bloqueos` de `05_progress/current.md`; revisar el `phaseProgress` de cada `finish_task` y atender los flags con `resolve_phase_review`.

## Salida esperada

Estado de tareas actualizado, progreso claro, y `ia_validate` en verde al terminar.
