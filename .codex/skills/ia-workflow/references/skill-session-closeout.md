---
title: IA Workflow Session Closeout Skill Reference
description: Referencia y checklist para crear el skill de cierre de sesión del proyecto que sincroniza /ia al final de una sesión.
---

## Propósito

El skill de cierre de sesión actualiza `/ia` al final de una sesión de trabajo para que la próxima sesión empiece sin reconstruir el contexto. Es el tercero de los tres skills de workflow que operan sobre `/ia`.

Crear este skill para que el progreso, las tareas, los issues, las decisiones y los aprendizajes se mantengan actualizados.

Las instrucciones generadas del proyecto deben mantener el contenido de `/ia` en el idioma del proyecto.

## Cuándo leer

* Al inicializar el workflow `/ia` en un proyecto nuevo.
* Al auditar si las sesiones persisten el estado de vuelta en `/ia` de forma confiable.
* Al adaptar los pasos de cierre tras cambios en la estructura de `/ia`.

## Pertenece a

* La secuencia de actualización al final de sesión sobre los archivos de `/ia`.
* La regla para archivar historial de progreso que ha crecido demasiado.
* El trigger para proponer un nuevo skill cuando aparece un patrón repetible.

## No pertenece a

* Reglas de creación de tareas. Esas pertenecen al skill de gestión de tareas.
* Veredictos de revisión. Esos pertenecen al skill de revisión de código.
* La estructura de `/ia`. Eso pertenece a `ia-workflow`.

## Frontmatter recomendado del skill

```yaml
---
name: project-session-closeout
description: Close a development session by updating tasks, progress, issues, ADRs and retrospective in /ia.
---
```

## Cuerpo recomendado del skill

* `Propósito`: persistir el estado de la sesión en `/ia`.
* `Cuándo usar`: al final de una sesión, o cuando el usuario diga que actualice `/ia`.
* `Contexto requerido`: ejecutar `ia_validate` e `ia_get_context(intent: "cerrar_sesion")`, y usar `ia_inspect` para la TASK, progreso, issues o decisiones pertinentes.
* `Procedimiento`: revisar cambios, cerrar tareas con `finish_task`, registrar progreso adicional con `ia_add_progress_entry`, registrar issues y ADRs con sus tools MCP, proponer un skill y archivar progreso con `archive_progress` cuando corresponda.
* `Reglas de seguridad`: sin secretos en ningún archivo; agregar al historial, no reescribirlo.
* `Salida esperada`: estado de tarea actualizado, progreso actual claro, trabajo pendiente y riesgos documentados.

## Secuencia de cierre

1. Revisar cambios con el estado del control de versiones.
2. Ejecutar `finish_task` en preview y con `apply: true` para cada TASK completada; el MCP actualiza el plan, la cola, el historial y el progreso.
3. Registrar progreso adicional con `ia_add_progress_entry` solo cuando no forma parte del cierre de una TASK.
4. Registrar bugs no resueltos con `ia_create_issue` y decisiones técnicas con `ia_create_decision`.
5. Proponer un nuevo skill si se detectó un patrón repetible.
6. Ejecutar `archive_progress` cuando la validación indique que el progreso requiere archivado.
7. Ejecutar `ia_validate` para confirmar que el cierre dejó `/ia` consistente.

## Errores comunes

* Cerrar una sesión sin mover las tareas completadas fuera de la cola activa.
* Dejar que `05_progress/current.md` crezca sin archivar.
* Registrar una decisión en las notas de progreso en vez de un ADR.
* Editar archivos o índices de `/ia` manualmente cuando `iaWorkflow` está disponible.
* No revisar si alguna fase de `03_plan.md` quedó completamente verde después de cerrar tareas.
