---
title: IA Workflow Task Management Skill Reference
description: Referencia y checklist para crear el skill de gestión de tareas del proyecto que opera /ia/04_tasks.
---

## Propósito

El skill de gestión de tareas gobierna cómo los agentes crean, seleccionan, actualizan, bloquean y archivan tareas accionables dentro de `/ia/04_tasks`. Es el primero de los tres skills de workflow que operan sobre `/ia`.

Crear este skill en cada proyecto que adopte `/ia` para que el ciclo de vida de las tareas se mantenga consistente y trazable.

## Cuándo leer

* Al inicializar el workflow `/ia` en un proyecto nuevo.
* Al auditar si las reglas del ciclo de vida de tareas existen y coinciden con la estructura de `/ia`.
* Al adaptar un skill de gestión de tareas existente a una nueva versión de `/ia`.

## Pertenece a

* Creación, selección y actualizaciones de estado de tareas.
* Formato de ID de tarea y reglas de unicidad.
* Movimiento entre estados activo, bloqueado y completado.
* El mapeo de área de tarea al skill de implementación que debe cargarse.

## No pertenece a

* La estructura de `/ia` en sí. Eso pertenece a `ia-workflow`.
* Implementación de código. Eso pertenece a los skills de área.
* Resumen de sesión. Eso pertenece al skill de cierre de sesión.

## Frontmatter recomendado del skill

```yaml
---
name: project-task-management
description: Manage actionable tasks in /ia/04_tasks - create, select, update status, block and archive tasks without mixing scopes.
---
```

Usar el nombre de la carpeta como `name` del skill.

## Cuerpo recomendado del skill

* `Propósito`: un párrafo sobre por qué existe el skill.
* `Cuándo usar` y `Cuándo no usar`: enrutar ideas sin criterios de aceptación a `04_tasks/backlog.md`.
* `Contexto requerido`: `04_tasks.md`, `04_tasks/current.md`, `templates/task-template.md`.
* `Procedimiento`: leer el índice, leer la cola activa, crear desde el template con un ID único, registrar el archivo de tarea como Borrador, validar/aprobar antes de Lista, actualizar la cola activa, manejar el bloqueo, la revisión y la completación.
* `Reglas`: cada tarea tiene Salida esperada verificable, alcance in/out claro, nivel de riesgo, estado de aprobación, ID inmutable, y las tareas completadas salen de la carpeta activa.
* `Mapa de área a skill`: una tabla que enlaza cada área de tarea al skill de implementación que un agente debe cargar antes de codificar.

## Convención genérica de ID de tarea

* Usar un patrón estable como `TASK-{INICIALES}-{AREA}-{NN}`.
* Calcular `{NN}` por autor y área antes de asignar, escaneando tanto las ubicaciones activas como las completadas.
* Mantener los IDs inmutables una vez asignados.

## Reglas del ciclo de vida

Aplicar el contrato canónico de [references/04-tasks.md](04-tasks.md), incluyendo estados,
riesgo, aprobación y archivo mensual de tareas completadas.

## Errores comunes

* Dejar que los archivos de tareas completadas se acumulen en la carpeta activa.
* Implementar tareas en borrador o no aprobadas.
* Tratar el trabajo de alto riesgo como aprobado solo porque existe un archivo de tarea.
* Mezclar varias funcionalidades grandes en una sola tarea.
* Codificar códigos de área del proyecto dentro de `ia-workflow` en vez de en este skill.
* Omitir el mapa de área a skill, haciendo que los agentes implementen sin cargar los skills de dominio.
