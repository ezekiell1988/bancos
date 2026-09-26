# Regla — Cierre de sesión

> Procedimiento genérico del workflow `/ia` (lo genera `create-ia-workflow`). Índice: [`README.md`](./README.md).

## Propósito

Actualizar `/ia` al final de una sesión de trabajo para que la próxima empiece sin reconstruir el contexto, sin duplicar escrituras.

## Cuándo aplicar

* Al final de una sesión de trabajo ("cerrar sesión", "fin de sesión", "registrar avance").
* Cuando el usuario pida explícitamente actualizar `/ia`.

No aplica a las reglas de creación de tareas ([`gestion-tareas.md`](./gestion-tareas.md)) ni a veredictos de revisión ([`revision-codigo.md`](./revision-codigo.md)).

## Contexto requerido

Ejecutar `ia_validate` e `ia_get_context(intent: "cerrar_sesion")` si el MCP está disponible; usar `ia_inspect` (o lectura directa) para la tarea, progreso, issues o decisiones pertinentes.

## Secuencia de cierre

1. Revisar los cambios con el estado del control de versiones.
2. Ejecutar `finish_task` en preview y con `apply: true` para cada tarea completada; sincroniza plan (fila `✅`, `> Avance` y, en la transición a 100 %, `✅ Completada` y flags), cola, historial y progreso (`Fase activa`, `En progreso`, `Completado en sesiones recientes`). Revisar el `phaseProgress` que devuelve.
   Para una tarea que sigue `En progreso` al cerrar: dejar su fila como está. Si quedó esperando algo externo, bloquearla con `work_task` (`transition: "blocked"`, `reason` y `nextStep`) y reanudarla con `resumed`. Si solo cambió el siguiente paso, no hay transición para eso: anotarlo en `Pendiente` (sección manual), sin editar la fila.
3. Registrar progreso adicional con `ia_add_progress_entry` solo cuando no forma parte del cierre de una tarea.
4. Registrar bugs no resueltos con `ia_create_issue` y decisiones técnicas con `ia_create_decision`.
5. Revisar cada flag arquitectónico o de retrospectiva en `02_architecture.md`/`08_retrospective.md`. Documentar la retrospectiva cuando haya evidencia suficiente, crear las tareas de sus acciones y resolver el flag con `resolve_phase_review`. Dejarlo pendiente solo si falta evidencia, con el motivo.
6. Si se detectó un patrón repetible, proponer una regla nueva en `rules/` (o en `## Ajustes del proyecto` de la regla que corresponda) o un skill técnico.
7. Actualizar a mano solo las secciones manuales de `05_progress/current.md`: `Resumen ejecutivo`, `Pendiente` y `Riesgos y bloqueos`.
8. Si `05_progress/current.md` supera 12 000 caracteres (`ia_validate` lo avisa), ejecutar `archive_progress` con `keepDays` (p. ej. 14): mueve al archivo mensual las viñetas fechadas más viejas y deja las recientes. Las viñetas sin fecha inicial no se archivan así: fecharlas antes con la fecha de cierre de su historial. Con `keepDays: 0` se mueve la sección completa y hay que reponerla con `Sin entradas registradas.`.
9. Ejecutar `ia_validate` para confirmar que el cierre dejó `/ia` consistente.

## Reglas de seguridad

* Sin secretos en ningún archivo.
* Agregar al historial, no reescribirlo.

## Salida esperada

Tareas cerradas con el MCP (que ya actualizó plan, `Fase activa`, `En progreso` y completado reciente) y, a mano, `Resumen ejecutivo`, `Pendiente` y `Riesgos y bloqueos` al día. Con MCP no se editan las líneas `> Fase activa` / `> Última actualización`, la tabla de `En progreso` ni las viñetas de completado.

## Errores comunes

* Cerrar una sesión sin mover las tareas completadas fuera de la cola activa.
* Dejar que `05_progress/current.md` crezca sin archivar.
* Registrar una decisión en las notas de progreso en vez de un ADR.
* Editar a mano el estado de una fase, la línea `> Avance`, `> Fase activa` o la tabla de `En progreso`: los mantiene el MCP.

## Ajustes del proyecto

Sin ajustes propios. (Esta sección es del proyecto: `--refresh-rules` la conserva.)
