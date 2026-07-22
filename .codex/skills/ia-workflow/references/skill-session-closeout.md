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
* `Contexto requerido`: `04_tasks/current.md`, `05_progress/current.md`, `07_issues.md`.
* `Procedimiento`: revisar cambios, actualizar tareas tocadas, actualizar progreso, registrar issues, registrar un ADR en `06_decisions/{ADR-ID}-{slug}.md` y actualizar `06_decisions.md` si se tomó una decisión, proponer un skill, archivar progreso antiguo.
* `Reglas de seguridad`: sin secretos en ningún archivo; agregar al historial, no reescribirlo.
* `Salida esperada`: estado de tarea actualizado, progreso actual claro, trabajo pendiente y riesgos documentados.

## Secuencia de cierre

1. Revisar cambios con el estado del control de versiones.
2. Actualizar cada archivo de tarea tocado y la cola activa; mover el trabajo completado al historial mensual.
3. Actualizar `05_progress/current.md` y el archivo del componente afectado.
4. Si alguna tarea completada aparecía en `03_plan.md` con `⏳ TASK-ID`, actualizar esa fila a `✅`. Si ya no quedan filas `⏳` en la fase, marcar el encabezado de fase como `✅ Completada`.
5. Registrar bugs no resueltos en `07_issues.md` usando el template de issue.
6. Registrar un ADR si se tomó una decisión técnica importante: crear un archivo bajo `06_decisions/{ADR-ID}-{slug}.md` y actualizar el índice en `06_decisions.md`.
7. Proponer un nuevo skill si se detectó un patrón repetible.
8. Archivar entradas de progreso antiguas cuando el archivo actual haya crecido demasiado.

## Errores comunes

* Cerrar una sesión sin mover las tareas completadas fuera de la cola activa.
* Dejar que `05_progress/current.md` crezca sin archivar.
* Registrar una decisión en las notas de progreso en vez de un ADR.
* Agregar cuerpos completos de ADR directamente en `06_decisions.md` en vez de usar un archivo por ADR.
* No revisar si alguna fase de `03_plan.md` quedó completamente verde después de cerrar tareas.
