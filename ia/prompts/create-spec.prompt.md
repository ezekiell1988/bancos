---
description: Convertir una necesidad en un requisito nuevo o actualizado en 01_requirements.md.
---

## Leer

* Con el MCP `iaWorkflow`: `ia_validate` e `ia_get_context` con `intent: "planificar"`; `01_requirements.md` con `ia_inspect`.
* Sin MCP (fallback): `ia/00_context.md`, `ia/01_requirements.md`.

## Objetivo

Traducir la necesidad planteada por el usuario en un requisito funcional verificable dentro de `01_requirements.md`.

## Reglas

* No inventar hechos del proyecto ni del negocio; marcar lo desconocido como pendiente de confirmación.
* Usar la numeración `REQ-NNN` existente; no reciclar IDs ya usados.
* Cada requisito nuevo tiene una señal de aceptación observable.

## Salida esperada

`01_requirements.md` actualizado con el requisito nuevo o modificado y su señal de aceptación.
