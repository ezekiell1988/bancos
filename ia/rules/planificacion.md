# Regla — Planificación de sesión

> Procedimiento genérico del workflow `/ia` (lo genera `create-ia-workflow`). Índice: [`README.md`](./README.md).

## Propósito

Orientar el inicio de una sesión de trabajo: qué leer, cómo evaluar prioridades y cómo seleccionar **una única** tarea aprobada para implementar, sin editar código ni archivos de `/ia` a mano.

## Cuándo aplicar

* Al inicio de una sesión sin tarea asignada ("qué hacemos hoy", "qué sigue", "próxima tarea").
* Cuando cambió el contexto, la prioridad, un riesgo o una dependencia.
* Cuando hay que decidir qué tarea continúa.

No aplica si ya hay una tarea `En progreso` y el trabajo sigue dentro de su alcance: leerla con `ia_inspect` y continuar con el skill técnico que asigna [`mapa-area-skill.md`](./mapa-area-skill.md). Para crear, aprobar o transicionar tareas fuera de la selección inicial, usar [`gestion-tareas.md`](./gestion-tareas.md).

## Contexto requerido

Ejecutar `ia_validate` e `ia_get_context(intent: "planificar")` si el MCP está disponible (con `iaWorkflow` configurado no se usa el modo manual); solo si no está disponible, leer `04_tasks/current.md`, `04_tasks/blocked.md` y `05_progress/current.md` directamente. `> Fase activa` y `## En progreso` de `05_progress/current.md` son el estado vivo (los mantiene el MCP); la fase `🔄 En curso` de `03_plan.md` y sus filas `⏳ TASK-ID` son el trabajo comprometido.

## Procedimiento

1. Ejecutar `ia_validate` e `ia_get_context` con `intent: "planificar"`, inicialmente en `summary`.
2. Usar `ia_inspect` para listar tareas, issues abiertos y, si hace falta, métricas o tareas archivadas.
3. Si existe una tarea `En progreso`, seleccionar esa tarea y no abrir otra.
4. Si no existe, seleccionar una tarea `Lista` aplicando el criterio de selección (abajo).
5. Leer la tarea elegida con `ia_inspect` y verificar alcance, criterios de aceptación, validación, rollback y dependencias.
6. Si la tarea no existe todavía, crearla según [`gestion-tareas.md`](./gestion-tareas.md); no implementar directamente desde `backlog.md`.
7. Confirmar aprobación y riesgo: una tarea `Borrador` no puede iniciar; una tarea de riesgo `Alto` necesita aprobación explícita del usuario.
8. Antes de editar, ejecutar `work_task` con `apply: true` para pasar la tarea de `Lista` a `En progreso`. Si ya estaba `En progreso`, no repetir la transición.

## Criterio de selección

Aplicar en este orden:

1. Tarea `En progreso` de la sesión actual.
2. Issue crítico que bloquee producción o un flujo principal.
3. Tarea `Lista` con fila `⏳ TASK-ID` en la fase `🔄 En curso`, empezando por prioridad `critica` o `alta`; después, otras `Lista` de prioridad `critica` o `alta`.
4. Tarea que desbloquee otras tareas.
5. Dependencia estructural necesaria para un feature planeado.
6. Tarea de menor riesgo si el tiempo disponible es limitado.

No asumir que una tarea está desbloqueada solo porque aparece en la cola: verificar sus dependencias y la condición externa registrada en su historial de eventos.

## Cambio de prioridades

No editar manualmente el orden de `current.md` cuando `iaWorkflow` esté activo. Si el cambio implica una decisión arquitectónica, usar `ia_create_decision`; si revela un bug o bloqueo no resuelto, usar `ia_create_issue`. Si el trabajo aún no está definido, crearlo en `Borrador` según [`gestion-tareas.md`](./gestion-tareas.md) hasta obtener aprobación y, si pertenece a la fase activa, asignarlo con `assign_task_to_phase`.

## Reglas de seguridad

* No implementar tareas `Borrador` ni items de backlog.
* No tomar más de una tarea por sesión.
* No omitir aprobación ni riesgo.
* No exponer secretos en consultas, tareas o respuestas.

## Salida esperada

Una única tarea elegida, leída por completo, con sus dependencias verificadas, aprobada y en `En progreso` antes de editar.

## Errores comunes

* Tomar más de una tarea activa por sesión.
* Implementar una tarea `Borrador` o de riesgo `Alto` sin aprobación explícita.
* Asumir que una tarea en la cola está desbloqueada sin revisar sus dependencias.
* Editar manualmente `current.md` para reordenar prioridades cuando `iaWorkflow` está disponible.
* Reescribir aquí el criterio de selección con reglas del proyecto: las prioridades propias van en `## Ajustes del proyecto`.

## Ajustes del proyecto

Sin ajustes propios. (Esta sección es del proyecto: `--refresh-rules` la conserva.)
