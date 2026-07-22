---
title: IA Workflow 03 Plan Example
description: Ejemplo y checklist para 03_plan.md en un workflow /ia genérico.
---

## Propósito

`03_plan.md` es el roadmap de desarrollo de alto nivel. Organiza el trabajo en fases y explica la dirección estratégica actual sin reemplazar las tareas accionables.

## Cuándo leer

* Al evaluar alcance.
* Al seleccionar o crear tareas.
* Al verificar si una solicitud encaja en la fase actual.
* Al reportar el estado por fases.

## Pertenece a

* Fases del proyecto
* Hitos principales
* Estado de fases
* Dependencias entre fases
* Criterios de éxito a alto nivel

## No pertenece a

* Pasos individuales de implementación. Usar `04_tasks/tasks/`.
* Detalles de progreso diario. Usar `05_progress/current.md`.
* Justificación arquitectónica. Usar `06_decisions/`.

## Quién lo actualiza

* Al **cerrar una tarea** (`finish_task` con `outcome=done`): si el ID de la tarea aparece en una fila de `03_plan.md`, el estado de esa fila cambia de `⏳ TASK-ID` a `✅`. Si no quedan filas `⏳` en la fase, el encabezado se marca como `✅ Completada`. El MCP aplica esta actualización automáticamente al detectar el ID en el plan.
* Al **agregar una fase nueva**: crear la sección con sus componentes en estado `⏳ TASK-ID` o `⏳ Pendiente`.
* Al **cerrar sesión**: revisar si alguna fase cambió de estado y actualizar el encabezado si corresponde.

## Esquema recomendado

```markdown
# 03 — Plan de Desarrollo

> Última actualización: {YYYY-MM-DD}

## Dirección actual

{un párrafo describiendo la fase activa y el objetivo}

## Fases

### Fase 1 — {nombre} {estado}

| Componente | Estado |
|------------|--------|
| {componente} | ⏳ TASK-XYZ / ✅ / ⏳ Pendiente |

Criterios de éxito:

* {resultado a nivel de fase}
* {validación a nivel de fase}

Dependencias:

* {dependencia o ninguna}

### Fase 2 — {nombre} {estado}

{misma estructura}

## Pendiente de planificación

* {idea que aún no está lista para convertirse en tarea}
```

## Checklist

* La fase activa es evidente.
* Cada fase tiene una señal de completitud medible.
* Los componentes son lo suficientemente amplios para planificación pero no vagos.
* El trabajo táctico está enlazado a tareas en vez de embebido aquí.
* Las ideas diferidas no se tratan como compromisos activos.
* Las filas de componentes se actualizan automáticamente cuando se cierran tareas con `finish_task`.

## Errores comunes

* Convertir el plan en un rastreador de tareas.
* Mantener el detalle de fases completadas para siempre en vez de resumirlo.
* Listar ideas futuras sin criterio de prioridad o conversión.
* No registrar las tareas con `⏳ TASK-ID` al crear la fase, impidiendo la actualización automática.
