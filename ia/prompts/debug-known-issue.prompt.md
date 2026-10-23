---
description: Investigar y avanzar un issue registrado en 07_issues.
---

## Leer

* Con el MCP `iaWorkflow`: `ia_validate` e `ia_get_context` con `intent: "depurar"`; el issue (`list_issues`/`read_file`) con `ia_inspect`.
* Sin MCP (fallback): `ia/00_context.md`, `ia/02_architecture.md`, el issue en `ia/07_issues/open/`.

## Objetivo

Investigar la causa raíz de un issue conocido y avanzar su resolución o workaround.

## Reglas

* Actualizar el issue con los hallazgos, no solo el código.
* Si la corrección requiere una tarea, crearla según `ia/rules/gestion-tareas.md` y enlazarla al issue con `ia_link_issue_to_task`.

## Salida esperada

El issue actualizado con causa raíz, workaround o corrección aplicada.
