# Regla — Revisión de código

> Procedimiento genérico del workflow `/ia` (lo genera `create-ia-workflow`). Índice: [`README.md`](./README.md). Reglas propias del proyecto: [`checklist-revision.md`](./checklist-revision.md).

## Propósito

Revisar un conjunto de cambios contra las reglas, decisiones y problemas conocidos registrados en `/ia` antes del commit o pull request.

## Cuándo aplicar

* Una tarea está completa y necesita verificarse antes de cerrarla.
* El usuario pide una revisión ("revisar código", "code review", "auditar diff", "validar PR").
* Corre un agente de QA sobre el diff actual.

No aplica para escribir la corrección (la revisión reporta hallazgos, no implementa salvo que se solicite), para el ciclo de vida de tareas ([`gestion-tareas.md`](./gestion-tareas.md)) ni para registrar una decisión nueva (se propone en el reporte).

## Contexto requerido

Ejecutar `ia_validate` e `ia_get_context(intent: "revisar")` si el MCP está disponible; si no, leer `02_architecture.md`, `06_decisions.md` y `07_issues.md` directamente. Usar `ia_inspect` (o lectura directa) solo para la tarea, ADRs, issues o documentos de `02_architecture/` pertinentes al diff.

## Procedimiento

1. Obtener el diff a revisar.
2. Verificar la Salida esperada de la tarea de origen (si el cambio viene de una tarea de `/ia`).
3. Aplicar las verificaciones generales (abajo) y cada punto de [`checklist-revision.md`](./checklist-revision.md). Si ese archivo no existe o sigue con marcadores POR DEFINIR, reportarlo como hallazgo y revisar solo con lo general.
4. Clasificar cada hallazgo: bloqueante, riesgo o mejora.
5. Emitir un veredicto final.

Si un hallazgo revela un defecto o una decisión necesaria, proponer `ia_create_issue` o `ia_create_decision` en el reporte; aplicar la mutación requiere autorización aparte y no sustituye el veredicto.

## Verificaciones generales

* Sin tokens, contraseñas, cadenas de conexión ni claves en el diff.
* El cambio no contradice un ADR vigente en `06_decisions.md`.
* Sin cambios fuera del `allowedScope` de la tarea.
* El conocimiento de `/ia` afectado (requisitos, arquitectura, ADR) está actualizado cuando la tarea lo requiere.
* Con el MCP disponible, el diff no contiene ediciones manuales de partes que mantiene el MCP: filas `⏳/🔄/✅ TASK-ID`, línea `> Avance` y estados de fase en `03_plan.md`; `> Fase activa`, `> Última actualización`, tabla de `En progreso` y viñetas de `Completado en sesiones recientes` en `05_progress/current.md`; `04_tasks/current.md`. Reportarlas como hallazgo de riesgo.

## Reglas de seguridad

* No corregir código silenciosamente durante la revisión.
* Nunca citar valores de secretos en el reporte; reportar solo su ubicación.

## Salida esperada

Un reporte con hallazgos bloqueantes, de riesgo y de mejora, y un veredicto final.

## Errores comunes

* Un checklist que se desfasa de los ADRs vigentes indexados en `06_decisions.md`.
* Citar un valor de secreto en el reporte en vez de apuntar a su ubicación.
* Revisar sin leer la tarea de origen, perdiendo la expansión de alcance.

## Ajustes del proyecto

Sin ajustes propios. (Esta sección es del proyecto: `--refresh-rules` la conserva.)
