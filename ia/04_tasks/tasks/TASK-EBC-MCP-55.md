# TASK-EBC-MCP-55 — Corregir estado de revisión en iaWorkflow

**Estado:** Borrador
**Autor:** Ezequiel Baltodano Cubillo `<pendiente>`
**Rama:** `dev`
**Fecha inicio:** 2026-07-31 15:43 CR
**Fecha cierre:** —
**Área:** MCP
**Prioridad:** alta
**Riesgo:** bajo
**Aprobación:** aprobada

---

## Título

Corregir estado de revisión en iaWorkflow

## Contexto

iaWorkflow y su skill modelan En revisión como estado canónico y finish_task(outcome=review) mueve allí las tareas. El flujo esperado es que una tarea pendiente de validación quede en Borrador, para requerir aprobación antes de volver a Lista.

## Objetivo

Eliminar En revisión como estado operativo y hacer que finish_task con outcome review deje la tarea en Borrador, con documentación y pruebas coherentes.

## Alcance permitido

* .mcp/ia-workflow/src/write-tools.mjs
* .mcp/ia-workflow/tools/finish_task.mjs
* .mcp/ia-workflow/README.md
* .agents/skills/ia-workflow/SKILL.md
* .agents/skills/ia-workflow/references/04-tasks.md
* .agents/skills/ia-workflow/references/local-mcp-vscode.md

## Fuera de alcance

* Cambiar datos financieros
* Leer o modificar secretos
* Modificar tareas existentes del proyecto fuera de los artefactos de workflow
* Cambiar el protocolo MCP

## Criterios de aceptación

* [ ] finish_task con outcome review escribe Estado: Borrador y registra la tarea bajo Borradores.
* [ ] El MCP ya no normaliza ni publica En revisión como estado operativo.
* [ ] El skill y las referencias describen Borrador como estado posterior a revisión y no presentan En revisión como estado canónico.
* [ ] El smoke test cubre la transición review a Borrador.
* [ ] node --check y el smoke test de ia-workflow pasan.

## Riesgos

Riesgo bajo.

## Archivos afectados / probables

* `.mcp/ia-workflow/src/write-tools.mjs`
* `.mcp/ia-workflow/tools/finish_task.mjs`
* `.mcp/ia-workflow/README.md`
* `.agents/skills/ia-workflow/SKILL.md`
* `.agents/skills/ia-workflow/references/04-tasks.md`
* `.agents/skills/ia-workflow/references/local-mcp-vscode.md`

## Plan técnico

1. Cambiar buildFinishTaskChanges para usar Borrador y la sección Borradores.
2. Eliminar el alias En revisión de normalizeTaskStatus y su heading operativo.
3. Actualizar las validaciones de cierre y los contratos publicados.
4. Actualizar README y referencias del skill.
5. Añadir un caso smoke que ejecute finish_task(review) sobre una tarea temporal y compruebe el estado Borrador.

## Pasos

1. Editar la transición y normalización del MCP.
2. Actualizar documentación y contrato de estados.
3. Ejecutar sintaxis y smoke.

## Salida esperada

El flujo iaWorkflow conserva las tareas pendientes de validación como Borrador y exige aprobación antes de implementarlas nuevamente.

## Validación

* [ ] node --check .mcp/ia-workflow/server.mjs
* [ ] node .mcp/ia-workflow/tests/smoke.mjs
* [ ] grep sin referencias operativas a En revisión en MCP y skill

## Rollback

Restaurar la transición review a En revisión y las referencias documentales anteriores.

## Dependencias

* ninguna

## Checklist

* [ ] Alcance revisado
* [ ] Riesgo revisado
* [ ] Aprobación registrada si aplica
* [ ] Implementación completa
* [ ] Validación completa
* [ ] Progreso/documentación actualizado

## Notas / contexto adicional

* Pendiente de revisión: finish_task review devuelve la tarea a Borrador y el smoke valida la nueva aprobación

* Aprobada por Ezequiel Baltodano Cubillo el 2026-07-31 15:43 CR.

No modificar tareas actuales del proyecto en esta corrección; el alcance es el comportamiento y la documentación del workflow.

## Issues vinculados

* ninguno
