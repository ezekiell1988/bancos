---
title: IA Workflow Schemas Reference
description: Referencia y checklist para ia/SCHEMAS.md en un workflow /ia genérico.
---

## Propósito

`SCHEMAS.md` almacena los templates completos de reconstrucción para los archivos de `/ia`. Los agentes lo usan cuando se inicializa, audita o repara un proyecto, no durante cada sesión de desarrollo normal.

Mantener `SCHEMAS.md` como archivo de referencia. El `/ia/README.md` debe permanecer pequeño y enrutar a los agentes al lugar correcto.

Todos los esquemas generados desde esta referencia deben producir contenido de `/ia` en el idioma del proyecto.

## Cuándo leer

* Al crear `/ia` desde cero.
* Al recrear un archivo `00` a `08` vacío o faltante.
* Al auditar si `/ia/README.md` se ha convertido en un repositorio de esquemas.
* Al actualizar templates de archivos compartidos tras un cambio de proceso.

## Pertenece a

* Templates completos de `00_context.md` a `08_retrospective.md`
* Reglas de reconstrucción compartidas para archivos vacíos o faltantes
* Forma del ID de tarea, estados del ciclo de vida, campos de riesgo/aprobación y catálogo genérico de áreas cuando aún no existe un skill de proyecto
* Punteros a skills especializados para detalles operativos

## No pertenece a

* Reglas de lectura diaria. Usar `/ia/README.md`.
* Trabajo activo. Usar `04_tasks/current.md` y archivos de tarea.
* Reglas detalladas del ciclo de vida de tareas cuando el proyecto tiene un skill de gestión de tareas.
* Convenciones de implementación específicas del proyecto. Usar skills de proyecto o ADRs.

## Esquema recomendado

```markdown
# /ia — Esquemas de Archivos

> Estructuras de referencia para crear o recrear archivos `/ia` vacíos o faltantes.
> No necesitas este archivo en sesiones normales.
> Para detalles del ciclo de vida de tareas, usar el skill de gestión de tareas del proyecto cuando esté disponible.

## `00_context.md` — Contexto del Proyecto

{template y notas}

## `01_requirements.md` — Requisitos del Sistema

{template y notas}

## `02_architecture.md` — Arquitectura del Sistema

{template y notas}

## `03_plan.md` — Plan de Desarrollo

{template y notas}

## `04_tasks.md` — Índice de Tareas

{template y notas}

## `05_progress.md` — Progreso del Proyecto

{template y notas}

## `06_decisions.md` — Decisiones Arquitectónicas

Usar el esquema canónico de [references/06-decisions.md](06-decisions.md). No duplicar la
plantilla de índice de ADR en este archivo.

## `07_issues.md` — Issues Conocidos

{template y notas}

## `08_retrospective.md` — Retrospectiva

{template y notas}
```

## Reglas de ubicación

* Colocar los templates completos en Markdown en `SCHEMAS.md`.
* Colocar las reglas de enrutamiento, lectura y puntos de entrada de skills en `README.md`.
* Colocar las reglas operativas del ciclo de vida de tareas en un skill de gestión de tareas del proyecto cuando exista.
* Incluir una referencia a [references/04-tasks.md](04-tasks.md) para los gates del ciclo de vida de tarea.
