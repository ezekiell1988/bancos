# Reglas del proyecto (`ia/rules/`)

Las reglas de trabajo de este proyecto viven aquí, dentro de su `/ia`. **No existen skills globales de workflow**: cada proyecto lleva sus propias reglas, que un agente lee con el MCP `iaWorkflow` (`ia_inspect action=read_file path=rules/<archivo>`) o directamente si el MCP no está disponible.

## Qué leer según la intención

| Intención | Regla | Además |
|---|---|---|
| Iniciar sesión, planificar, elegir la próxima tarea | [`planificacion.md`](./planificacion.md) | — |
| Crear, aprobar, iniciar, bloquear o cerrar una tarea | [`gestion-tareas.md`](./gestion-tareas.md) | [`mapa-area-skill.md`](./mapa-area-skill.md) antes de implementar |
| Revisar un diff o un cambio antes de commit/PR | [`revision-codigo.md`](./revision-codigo.md) | [`checklist-revision.md`](./checklist-revision.md) |
| Cerrar la sesión y dejar `/ia` al día | [`cierre-sesion.md`](./cierre-sesion.md) | — |

## Tipos de archivo

| Archivo | Tipo | Quién lo mantiene |
|---|---|---|
| `README.md`, `planificacion.md`, `gestion-tareas.md`, `revision-codigo.md`, `cierre-sesion.md` | Procedimiento genérico del workflow `/ia` | Se generan con `create-ia-workflow` y se actualizan con `node <skill>/scripts/init-ia.mjs --root <proyecto> --refresh-rules`. Un ajuste propio del proyecto va en la sección `## Ajustes del proyecto` del archivo, que el refresco conserva. |
| `checklist-revision.md` | Regla propia del proyecto | El proyecto: reglas verificables derivadas de su stack, ADRs y convenciones. |
| `mapa-area-skill.md` | Regla propia del proyecto | El proyecto: qué skill técnico cargar por área antes de implementar. |

## Reglas comunes

* El MCP `iaWorkflow` es la interfaz de `/ia`: `ia_validate` + `ia_get_context` al empezar; `work_task` antes de editar y `finish_task` al cerrar.
* No editar a mano las partes que mantiene el MCP (filas `TASK-ID`, `> Avance` y estados de fase en `03_plan.md`; `> Última actualización`, `> Fase activa`, `En progreso` y completado reciente en `05_progress/current.md`; `04_tasks/current.md`).
* Con `iaWorkflow` configurado no se usa el modo manual "sin MCP" de ninguna regla: ese modo es solo un respaldo si el MCP no está disponible.
* Sin secretos, tokens, contraseñas ni cadenas de conexión en ningún archivo, commit o respuesta.

## Ajustes del proyecto

Sin reglas propias adicionales. Si el proyecto agrega otro archivo de reglas en `rules/`, enlazarlo aquí con su intención. (Esta sección es del proyecto: `--refresh-rules` la conserva.)
