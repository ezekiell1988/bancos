# /ia — Sistema de Contexto Estructurado para LLMs

Esta carpeta es la fuente de verdad para agentes.

> Las reglas del workflow (planificación, gestión de tareas, revisión y cierre de sesión) viven en
> `rules/` de este proyecto; no hay skills globales de workflow. Índice: `rules/README.md`.
> Los 9 prompts del workflow viven en `prompts/` (única copia del proyecto). Instrucciones de
> asistentes y comandos de Claude son globales: el proyecto no necesita archivos fuera de `ia/`.

## Índice de archivos

| Archivo | Propósito | Leer cuando |
|---------|----------|--------------|
| `00_context.md` | Identidad estable, stack y constantes críticas | Siempre, al iniciar sesión |
| `01_requirements.md` | Comportamiento del negocio e invariantes | Al planificar o revisar reglas |
| `02_architecture.md` | Componentes, flujo de datos y contratos técnicos | Al implementar o revisar límites |
| `03_plan.md` | Roadmap por fases y dirección actual | Al evaluar alcance o seleccionar trabajo |
| `04_tasks.md` | Índice del sistema operativo de tareas | Al crear, iniciar o cerrar una tarea |
| `05_progress.md` | Estado actual e histórico | Al iniciar o cerrar sesión |
| `06_decisions.md` | Índice de ADRs | Al cambiar arquitectura o revisar decisiones previas |
| `07_issues.md` | Bugs y limitaciones conocidas | Al depurar o revisar código frágil |
| `08_retrospective.md` | Aprendizajes de fases completadas | Al cerrar una fase o crear un skill nuevo |

## Reglas de workflow (`rules/`)

Con el MCP se leen con `ia_inspect` (`action: "read_file"`, `path: "rules/<archivo>"`); sin MCP, directo.

| Momento | Regla | Además |
|---------|-------|--------|
| Planificar sesión / elegir tarea | `rules/planificacion.md` | — |
| Crear o actualizar tareas | `rules/gestion-tareas.md` | `rules/mapa-area-skill.md` antes de implementar |
| Revisar cambios | `rules/revision-codigo.md` | `rules/checklist-revision.md` |
| Cerrar sesión | `rules/cierre-sesion.md` | — |

Las reglas genéricas se actualizan con `init-ia.mjs --refresh-rules` del skill `create-ia-workflow`,
que conserva la sección final `## Ajustes del proyecto` de cada una. `checklist-revision.md` y
`mapa-area-skill.md` son propias del proyecto y nunca se sobrescriben.

## Flujo delegado

Las tareas son el contrato entre humano y agente. El ciclo de vida, los estados, el riesgo y la
aprobación están en `rules/gestion-tareas.md`; el contrato canónico es `references/04-tasks.md`
del skill `ia-workflow`.

Cuando existe una fase activa, asignar cada tarea comprometida con `assign_task_to_phase`; no
editar manualmente sus filas en `03_plan.md`. Un issue posterior normalmente genera una tarea
de corrección nueva. Usar `reopen_task` solo si un issue abierto invalida el cierre original.

Si el proyecto habilita el MCP `iaWorkflow`, este README deja de ser el punto de entrada operativo:
las tools `ia_get_context` / `ia_validate` / ciclo de tareas lo reemplazan. Contrato de `/ia`: skill
`ia-workflow`; contrato técnico del MCP: `references/local-mcp-vscode.md` del skill `create-ia-workflow`.

## Automatización del avance

Con el MCP, `assign_task_to_phase`, `work_task` y `finish_task` mantienen solos, en el mismo commit
que la transición de la tarea:

* `03_plan.md`: las filas `⏳/🔄/✅ TASK-ID` de la tabla `| Componente | Estado |`, la línea
  `> Avance: N de M componentes completados (…)` y el estado de la fase (`⏳ Pendiente` →
  `🔄 En curso` → `✅ Completada`, con flags de revisión en `02` y `08` al completarse).
* `05_progress/current.md`: `> Última actualización`, `> Fase activa`, la tabla de `## En progreso`
  y las viñetas de `## Completado en sesiones recientes`.

No editar esas partes a mano. Reglas: una sola fase `🔄 En curso`; `###` solo para fases (subsecciones
en `####` o negrita); un `TASK-ID` por fila. Detalle: `references/automatizacion-avance.md` del skill
`ia-workflow`.

## Esquemas de archivos

Los esquemas completos para crear archivos vacíos viven en `ia/SCHEMAS.md`.

## Estructura de carpetas

| Carpeta | Contenido |
|---------|-----------|
| `04_tasks/` | Activo, backlog, bloqueado, archivos de tarea y archivo mensual de completadas |
| `05_progress/` | Estado actual y archivo por componente |
| `06_decisions/` | Un archivo por ADR: `ADR-XX-{slug}.md` |
| `07_issues/` | Issues abiertos y archivados |
| `rules/` | Reglas del workflow: genéricas (se refrescan) y propias del proyecto |
| `prompts/` | Los 9 prompts del workflow (`*.prompt.md`) |

## Reglas de lectura para agentes

Con el MCP, usar `ia_get_context` con la intención correspondiente e `ia_inspect` para lecturas
puntuales (incluida la regla de la intención). Las lecturas directas de abajo son el fallback cuando
el MCP no está disponible.

### Planificación

Leer: `00_context.md`, `01_requirements.md`, `03_plan.md`. Regla: `rules/planificacion.md`.

### Implementación

Leer: `00_context.md`, `02_architecture.md`, la tarea activa en `04_tasks/tasks/`. Reglas:
`rules/gestion-tareas.md` y `rules/mapa-area-skill.md` (skill técnico del área).

### Revisión

Leer: `02_architecture.md`, `06_decisions.md`, `07_issues.md`. Reglas: `rules/revision-codigo.md`
y `rules/checklist-revision.md`.

### Depuración

Leer: `00_context.md`, `02_architecture.md`, el issue relacionado en `07_issues/open/`.

### Cierre de sesión

Regla: `rules/cierre-sesion.md`.
Con MCP: `finish_task` por cada tarea completada, `ia_add_progress_entry` para avances que no cierran
tarea e `ia_create_issue`/`ia_create_decision`/`resolve_phase_review` según corresponda. No editar
`03_plan.md` (filas, línea `> Avance`, estados de fase) ni las partes gestionadas de
`05_progress/current.md`; a mano solo `Resumen ejecutivo`, `Pendiente` y `Riesgos y bloqueos`.
Sin MCP: actualizar la tarea, `03_plan.md` y `05_progress/current.md` con el mismo formato.

## Reglas de mantenimiento

1. No inventar hechos del proyecto; marcar lo desconocido como pendiente de confirmación.
2. Mantener cada archivo del `00` al `08` dentro de su límite de propiedad y tamaño.
