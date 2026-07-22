---
title: IA Workflow README Reference
description: Referencia y checklist para ia/README.md en un workflow /ia genérico.
---

## Propósito

`README.md` es el punto de entrada para agentes que trabajan con `/ia`. Debe explicar cómo navegar el sistema de contexto, qué archivos leer según la intención y qué skills controlan los flujos de trabajo estándar.

Mantener este archivo pequeño. Es un enrutador, no la base de conocimiento completa.

Todo el contenido generado de `/ia` debe escribirse en el idioma del proyecto.

## Cuándo leer

* Al crear `/ia` desde cero.
* Al auditar si los agentes pueden comenzar desde `/ia` sin leer todo.
* Al compactar un `/ia/README.md` demasiado grande.
* Al cambiar las reglas de lectura o el mapa de skills de workflow.

## Pertenece a

* El propósito de `/ia`
* El índice de archivos y carpetas
* Reglas de lectura por intención
* Skills de workflow requeridos y dónde viven las reglas detalladas
* Resumen del flujo delegado y gate de aprobación de tareas cuando el proyecto usa agentes/MCP
* Reglas de mantenimiento que aplican a toda la carpeta

## No pertenece a

* Esquemas completos de archivos. Usar `SCHEMAS.md`.
* Reglas detalladas del ciclo de vida de tareas. Usar el skill de gestión de tareas del proyecto.
* Estado activo de tareas. Usar `04_tasks/current.md`.
* Historial de progreso. Usar `05_progress/`.
* Contenido de ADRs. Usar un archivo por ADR en `06_decisions/`.

## Esquema recomendado

```markdown
# /ia — Sistema de Contexto Estructurado para LLMs

Esta carpeta es la fuente de verdad para agentes.

> Skills de workflow requeridos: {nota breve y nombres de skills}

## Índice de archivos

| Archivo | Propósito | Leer cuando |
|---------|----------|--------------|
| `00_context.md` | {propósito} | {intención} |
| `01_requirements.md` | {propósito} | {intención} |
| `02_architecture.md` | {propósito} | {intención} |
| `03_plan.md` | {propósito} | {intención} |
| `04_tasks.md` | {propósito} | {intención} |
| `05_progress.md` | {propósito} | {intención} |
| `06_decisions.md` | {propósito} | {intención} |
| `07_issues.md` | {propósito} | {intención} |
| `08_retrospective.md` | {propósito} | {intención} |

## Skills de workflow

| Momento | Skill |
|---------|-------|
| Crear o actualizar tareas | `{skill-gestión-de-tareas}` |
| Revisar cambios | `{skill-revisión-de-código}` |
| Cerrar sesión | `{skill-cierre-de-sesión}` |

## Flujo delegado

Las tareas son el contrato entre humano y agente. Para el ciclo de vida, los estados, el riesgo y
la aprobación, enlazar al skill de gestión de tareas del proyecto o a
`references/04-tasks.md` cuando el proyecto aún no tenga uno.

## Esquemas de archivos

Los esquemas completos para crear archivos vacíos viven en `ia/SCHEMAS.md`.

## Estructura de carpetas

| Carpeta | Contenido |
|---------|-----------|
| `04_tasks/` | {activo, backlog, bloqueado, archivos de tarea y archivo mensual de completadas} |
| `05_progress/` | {estado actual y archivo} |
| `06_decisions/` | {un archivo por ADR: ADR-XX-{slug}.md} |
| `07_issues/` | {issues abiertos y archivados} |

## Reglas de lectura para agentes

### Planificación

Leer: {archivos mínimos}

### Implementación

Leer: {archivos mínimos y skills requeridos}

### Revisión

Leer: {archivos mínimos y skills requeridos}

### Depuración

Leer: {archivos mínimos y contexto del issue}

### Cierre de sesión

Actualizar: {tarea, progreso, issues, decisiones y retrospectiva según aplique}

## Reglas de mantenimiento

1. {regla crítica}
2. {regla crítica}
```

## Reglas de ubicación

* Mantener las reglas de navegación y lectura por intención en `README.md`.
* Mover los esquemas largos a `SCHEMAS.md`.
* Mover los procedimientos operativos a skills.
* Mover los hechos del proyecto a los archivos numerados del componente.
* Enlazar a referencias en vez de copiar secciones grandes.
* Mantener el contenido generado en el idioma del proyecto.

## Checklist

* Un agente nuevo puede decidir qué leer sin escanear toda la carpeta.
* El índice de archivos cubre cada componente del `00` al `08`.
* Los skills de workflow requeridos están nombrados explícitamente.
* Los flujos delegados declaran que las tareas Borrador no pueden implementarse.
* La aprobación de tareas de riesgo alto es visible antes de la implementación.
* Los detalles de esquema están enlazados, no duplicados.
* Las reglas de ciclo de vida de carpetas están resumidas pero no repetidas exhaustivamente.
* El README es lo suficientemente corto para cargarse al inicio de sesión.

## Errores comunes

* Convertir `README.md` en un catálogo completo de esquemas.
* Copiar procedimientos enteros de skills en el README.
* No dejar puntero a los skills de gestión de tareas, revisión o cierre de sesión.
* Mezclar el estado actual del proyecto en el punto de entrada.

