---
description: Documentar la retrospectiva de una fase cerrada y convertir sus acciones en tareas.
---

## Leer

* Con el MCP `iaWorkflow`: `ia_validate` e `ia_get_context` con `intent: "cerrar_sesion"`; `02_architecture.md` y `08_retrospective.md` con `ia_inspect`.
* Sin MCP (fallback): `ia/02_architecture.md` y `ia/08_retrospective.md` (sección «Fases pendientes de …»), `ia/03_plan.md` (fase completada).

## Objetivo

Registrar los aprendizajes de una fase recién completada y convertirlos en trabajo accionable.

## Reglas

* No resolver el flag de retrospectiva con `resolve_phase_review` antes de documentarla con evidencia.
* Cada acción concreta de la retrospectiva se convierte en una tarea, ADR, regla de `ia/rules/` o skill técnico, no solo en prosa.
* Crear las tareas según `ia/rules/gestion-tareas.md`.

## Salida esperada

La retrospectiva registrada, sus acciones convertidas en tareas/ADRs/reglas/skills, y el flag resuelto.
