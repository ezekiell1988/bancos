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
* `Procedimiento`: obtener contexto mediante el MCP, crear la tarea con `create_task`, aprobarla, iniciarla con `work_task`, y cerrarla con `finish_task`. El skill no prescribe actualizaciones manuales de archivos ni índices.
* `Reglas`: cada tarea tiene Salida esperada verificable, alcance in/out claro, nivel de riesgo, estado de aprobación, ID inmutable, y las tareas completadas salen de la carpeta activa.
* `Mapa de área a skill`: una tabla que enlaza cada área de tarea al skill de implementación que un agente debe cargar antes de codificar.

## Convención genérica de ID de tarea

* `create_task` asigna un ID estable con el patrón `TASK-{INICIALES}-{AREA}-{NN}`.
* El agente proporciona `authorInitials`; el MCP calcula el consecutivo considerando tareas activas e históricas.
* Mantener los IDs inmutables una vez asignados.

## Reglas del ciclo de vida

Aplicar el contrato canónico de [references/04-tasks.md](04-tasks.md), incluyendo estados,
riesgo, aprobación y archivo mensual de tareas completadas.

## Contrato MCP para crear tareas

Cuando `iaWorkflow` esté disponible, el skill debe usar las acciones públicas en lugar de crear
archivos o actualizar índices manualmente. El orden es: `create_task` en preview,
`create_task` con `apply: true`, `approve_task` con `apply: true` y `work_task` con
`apply: true` antes de editar código o documentación.

`finish_task` cierra la tarea como `Completada`, actualiza el plan, la cola y el progreso, y la mueve al historial. No existe una transición de revisión.

Si el trabajo se bloquea por una dependencia externa, usar `work_task` con `transition: "blocked"`, `reason` y `apply: true`. Para continuar, usar `transition: "resumed"` con un nuevo `reason`. El MCP registra el historial y actualiza `current.md` y `blocked.md`; no editar esos archivos manualmente.

`create_task` publica los siguientes campos obligatorios:

| Campo | Tipo | Uso |
|-------|------|-----|
| `title` | string | Título corto de la tarea |
| `area` | enum | `FE`, `BE`, `HF`, `DB`, `INF`, `DOC`, `MCP`, `ARCH`, `QA` o `CAP` |
| `context` | string | Motivo y contexto del trabajo |
| `objective` | string | Resultado buscado |
| `allowedScope` | string[] | Límites incluidos |
| `outOfScope` | string[] | Límites excluidos |
| `acceptanceCriteria` | string[] | Resultados verificables |
| `technicalPlan` | string[] | Plan técnico |
| `steps` | string[] | Pasos de ejecución |
| `expectedOutput` | string | Salida final verificable |
| `validation` | string[] | Comandos o verificaciones |
| `rollback` | string | Procedimiento de reversión |

También acepta estos campos opcionales:

| Campo | Tipo | Valor predeterminado o uso |
|-------|------|-----------------------------|
| `priority` | enum | `media`; permite `critica`, `alta`, `media` o `baja` |
| `risk` | enum | `medio`; permite `bajo`, `medio` o `alto` |
| `authorName` | string | Nombre configurado en Git |
| `authorEmail` | string | Correo configurado en Git |
| `authorInitials` | string | Iniciales para el ID; admite sufijo numérico ante colisión |
| `branch` | string | `dev` |
| `likelyFiles` | string[] | Archivos afectados o pendientes de confirmar |
| `dependencies` | string[] | `ninguna` |
| `notes` | string | Notas adicionales |
| `apply` | boolean | `false`; solo `true` persiste el borrador |

No enviar `status` ni `approval` a `create_task`: no forman parte de su schema público. La tool
crea la tarea en `Borrador`; `approve_task` registra `Lista` y la aprobación correspondiente.
`approve_task` requiere `id` y acepta `approver` y `apply`. `work_task` requiere `id`, acepta
`mode` (`summary` o `full`), `maxChars`, `transition`, `reason` y `apply`; sin transición,
`apply: true` cambia una tarea `Lista` a `En progreso`. Con `transition: "blocked"` o
`transition: "resumed"`, registra el evento temporal correspondiente.

## Errores comunes

* Dejar que los archivos de tareas completadas se acumulen en la carpeta activa.
* Implementar tareas en borrador o no aprobadas.
* Tratar el trabajo de alto riesgo como aprobado solo porque existe un archivo de tarea.
* Mezclar varias funcionalidades grandes en una sola tarea.
* Codificar códigos de área del proyecto dentro de `ia-workflow` en vez de en este skill.
* Omitir el mapa de área a skill, haciendo que los agentes implementen sin cargar los skills de dominio.
* Actualizar manualmente archivos de tarea, índices o progreso cuando `iaWorkflow` está disponible.
