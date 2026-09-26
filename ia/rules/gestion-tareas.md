# Regla — Gestión de tareas

> Procedimiento genérico del workflow `/ia` (lo genera `create-ia-workflow`). Índice: [`README.md`](./README.md).

## Propósito

Gobernar cómo se crean, seleccionan, actualizan, bloquean y archivan las tareas accionables de `/ia/04_tasks`, para que el ciclo de vida sea consistente y trazable sin mezclar alcances.

## Cuándo aplicar

* Al crear una tarea nueva ("nueva tarea", "crear task").
* Al seleccionar trabajo para implementar.
* Al actualizar el estado de una tarea (aprobar, iniciar, bloquear, reanudar, cerrar).

No aplica a ideas sin criterios de aceptación (van a `04_tasks/backlog.md`, no a una tarea), a la implementación de código (cargar el skill técnico que asigna [`mapa-area-skill.md`](./mapa-area-skill.md)) ni al cierre de sesión ([`cierre-sesion.md`](./cierre-sesion.md)).

## Contexto requerido

Con el MCP: `ia_validate`, `ia_get_context` y, para lecturas puntuales (la tarea, `rules/`), `ia_inspect`. Sin MCP: `04_tasks.md`, `04_tasks/current.md` y `templates/task-template.md`.

## Procedimiento

1. Obtener contexto con el MCP `iaWorkflow` (`ia_validate` + `ia_get_context`); si no está disponible, leer `04_tasks/current.md` y `03_plan.md` directamente.
2. Crear la tarea con `create_task` en preview, revisarla, y luego con `apply: true` (no enviar `status` ni `approval`: toda tarea nace en `Borrador`). El primer paso de `steps` debe ser una acción concreta: se publica como `Siguiente paso` en `05_progress/current.md` al iniciar. Si la respuesta trae `warnings`/`undefinedTaskReferences`, corregir la cita o crear antes la tarea citada; no citar TASK-IDs futuros.
3. Si pertenece a una fase, asignarla con `assign_task_to_phase` (una fila `⏳ TASK-ID` por tarea; nunca editar `03_plan.md` manualmente).
4. Aprobarla con `approve_task` cuando su alcance y riesgo estén validados (riesgo `alto` exige aprobador explícito).
5. Iniciarla con `work_task` antes de editar cualquier archivo del proyecto (acepta `nextStep`). Si avisa que su fase «sigue ⏳ Pendiente» porque otra está en curso, es correcto (su fila ya está `🔄`); cambiar la fase vigente es una decisión del plan, no algo que se arregla a mano.
6. Antes de implementar, leer [`mapa-area-skill.md`](./mapa-area-skill.md) y cargar el skill técnico de su área. Si el archivo no existe o el área no está mapeada, avisar al usuario y proponer completarlo en lugar de implementar sin skill de dominio.
7. Si se bloquea por una dependencia externa, usar `work_task` con `transition: "blocked"` y `reason`; para continuar, `transition: "resumed"` con un nuevo `reason` (ambas aceptan `nextStep`).
8. Cerrarla con `finish_task`, que sincroniza plan (fila, `> Avance`, estado de la fase), cola y progreso (`Fase activa`, `En progreso`, completado reciente); no retocar esas partes a mano.
9. Si un issue tardío invalida un cierre, usar `reopen_task`: la tarea vuelve a `Borrador` y hay que aprobarla otra vez con `approve_task`. Si no lo invalida, crear una tarea de corrección nueva y enlazarla con `ia_link_issue_to_task`. Una tarea borrada por error se recupera con `restore_task`.

No existe una transición de revisión: la revisión se hace con [`revision-codigo.md`](./revision-codigo.md) antes de `finish_task`. Las tareas V1 solo se migran con `migrate_tasks_to_v2` (preview primero).

Solo si el MCP no está disponible, seguir el mismo ciclo de vida editando `04_tasks/` a mano según el contrato de tareas V2 (`SCHEMAS.md` y `templates/task-template.md` del proyecto), y mantener también a mano, con el mismo formato, la fila y la línea `> Avance` de `03_plan.md` y `> Fase activa` y la tabla de `En progreso` de `05_progress/current.md`, para que la `/ia` siga compatible cuando vuelva el MCP.

## Reglas

* Cada tarea nueva usa el contrato V2 (`schemaVersion: 2`, timestamps RFC 3339, eventos JSONL).
* Toda tarea tiene Salida esperada verificable, alcance in/out claro, nivel de riesgo, estado de aprobación e ID inmutable.
* Una tarea pertenece a un solo proyecto: no mezclar proyectos en una tarea, issue o ADR.
* Las tareas completadas salen de `04_tasks/tasks/` hacia `04_tasks/done/YYYY-MM.md` (lo hace `finish_task`).
* Nunca implementar una tarea en `Borrador` ni de riesgo alto sin aprobación explícita.
* No actualizar manualmente archivos de tarea, índices o progreso cuando `iaWorkflow` está disponible.

## Errores comunes

* Dejar que los archivos de tareas completadas se acumulen en la carpeta activa.
* Implementar tareas en borrador o no aprobadas.
* Mezclar varias funcionalidades grandes en una sola tarea.
* Omitir el mapa de área a skill, haciendo que se implemente sin cargar el skill de dominio.

## Ajustes del proyecto

Sin ajustes propios. (Esta sección es del proyecto: `--refresh-rules` la conserva.)
