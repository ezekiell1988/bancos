# /ia — Esquemas de Archivos

> Estructuras de referencia para crear o recrear archivos `/ia` vacíos o faltantes.
> No necesitas este archivo en sesiones normales.
> Para detalles del ciclo de vida de tareas, usar `rules/gestion-tareas.md` y el contrato
> `references/04-tasks.md` del skill `ia-workflow`.

## `00_context.md` — Contexto del Proyecto

Ver `references/00-context.md` del skill `ia-workflow`.

## `01_requirements.md` — Requisitos del Sistema

Ver `references/01-requirements.md` del skill `ia-workflow`.

## `02_architecture.md` — Arquitectura del Sistema

Ver `references/02-architecture.md` del skill `ia-workflow`.

## `03_plan.md` — Plan de Desarrollo

Contrato completo en `references/03-plan.md` del skill `ia-workflow`. Lo que lee el MCP:

* Cada `###` es una fase: `### {Nombre} {estado}` con estado `⏳ Pendiente`, `🔄 En curso`, `⏸️ Diferida`,
  `⛔ Bloqueada` o `✅ Completada`. Una sola fase `🔄 En curso`. Subsecciones en `####` o negrita.
* Tabla canónica `| Componente | Estado |` con un `TASK-ID` por fila (`⏳`/`🔄`/`✅ TASK-ID`).
* `> Avance: …` y los estados de fila y fase los escribe el MCP.
* `> Requisitos: REQ-…` debajo del encabezado: de qué requisitos de `01` sale la fase (`extend_main_plan` con `requirements`).

```markdown
# 03 — Plan de Desarrollo

> Última actualización: {AAAA-MM-DD}

## Dirección actual

{fase activa, objetivo y siguiente hito}

## Fases

### Fase 1 — {nombre} 🔄 En curso

> Avance: 0 de 2 componentes completados (actualizado {AAAA-MM-DD}).
> Requisitos: REQ-001

| Componente | Estado |
|------------|--------|
| {componente} | ⏳ TASK-{INI}-{AREA}-01 |
| {componente} | ⏳ TASK-{INI}-{AREA}-02 |

**Criterios de éxito:**

* {resultado verificable}

**Dependencias:**

* {dependencia o ninguna}

### Fase 2 — {nombre} ⏳ Pendiente

## Pendiente de planificación

* {idea que aún no está lista para convertirse en tarea}
```

## `04_tasks.md` — Índice de Tareas

Ver `references/04-tasks.md` del skill `ia-workflow`.

## `05_progress.md` — Progreso del Proyecto

Contrato completo en `references/05-progress.md` del skill `ia-workflow`. Con el MCP, las dos líneas del
encabezado, `## Completado en sesiones recientes` y `## En progreso` son gestionadas; a mano solo
`Resumen ejecutivo`, `Pendiente` y `Riesgos y bloqueos`. `Fase activa` empieza por el nombre exacto de la
fase del plan.

```markdown
# 05 — Progreso Actual

> Última actualización: {AAAA-MM-DD HH:MM} CR
> Fase activa: Fase 1 — {nombre} (0 de 2 componentes completados)

## Resumen ejecutivo

{estado actual breve y próxima prioridad}

## Completado en sesiones recientes

Sin entradas registradas.

## En progreso

| Item | Responsable | Estado | Siguiente paso |
|------|-------------|--------|----------------|
| — | — | — | — |

## Pendiente

| Prioridad | Item | Razón |
|-----------|------|-------|

## Riesgos y bloqueos

| Riesgo | Impacto | Mitigación |
|--------|---------|------------|
```

`05_progress/by-component/<slug>.md` usa `## Completado en sesiones recientes` (ahí escriben
`ia_add_progress_entry` y `close_issue` con `component`).

## `06_decisions.md` — Decisiones Arquitectónicas

Usar el esquema canónico de `references/06-decisions.md` del skill `ia-workflow`. No duplicar
la plantilla de índice de ADR en este archivo.

## `07_issues.md` — Issues Conocidos

Ver `references/07-issues.md` del skill `ia-workflow`.

## `08_retrospective.md` — Retrospectiva

Ver `references/08-retrospective.md` del skill `ia-workflow`.

## `rules/` — Reglas del workflow

* Genéricas: `README.md`, `planificacion.md`, `gestion-tareas.md`, `revision-codigo.md` y
  `cierre-sesion.md`. Para recrearlas o actualizarlas: `node <skill create-ia-workflow>/scripts/init-ia.mjs
  --root <proyecto> --refresh-rules` (conserva la sección final `## Ajustes del proyecto` de cada una).
* Propias: `checklist-revision.md` (reglas verificables de revisión) y `mapa-area-skill.md` (tabla
  `| Área | Alcance | Skill(s) de implementación |`). Se recrean a mano; el refresco nunca las toca.
* Contrato completo: `references/rules.md` del skill `create-ia-workflow`.
