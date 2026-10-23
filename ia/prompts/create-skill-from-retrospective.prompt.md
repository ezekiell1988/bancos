---
description: Convertir un aprendizaje repetible de la retrospectiva en un skill nuevo del proyecto.
---

## Leer

* Con el MCP `iaWorkflow`: `ia_validate` e `ia_get_context` con `intent: "planificar"`; `08_retrospective.md` con `ia_inspect`.
* Sin MCP (fallback): `ia/08_retrospective.md`.

## Objetivo

Empaquetar un patrón técnico repetible detectado en la retrospectiva como un skill reutilizable.

## Reglas

* Si el patrón es del workflow del proyecto (cómo planificar, gestionar tareas, revisar o cerrar), no crear un skill: registrarlo como regla en `ia/rules/` (archivo propio o sección `## Ajustes del proyecto` de la regla genérica que corresponda).
* No duplicar un skill ya existente: extenderlo si el patrón encaja en uno. Si crea un skill técnico, mapear su área en `ia/rules/mapa-area-skill.md`.
* Seguir el skill `create-skill` si el proyecto lo tiene disponible, o `ia/templates/skill-template.md` en su defecto.

## Salida esperada

Un skill nuevo creado y referenciado desde donde corresponda (README, mapa de área, etc.).
