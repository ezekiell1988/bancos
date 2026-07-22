---
title: IA Workflow 05 Progress Example
description: Ejemplo y checklist para 05_progress.md y 05_progress/ en un workflow /ia genérico.
---

## Propósito

`05_progress.md` y `05_progress/` le dicen al próximo agente qué está hecho, qué está pendiente y dónde se necesita atención. El progreso está orientado a resultados, no es un registro de cambios crudo.

## Cuándo leer

* Al iniciar una sesión y verificar el estado actual.
* Al cerrar una sesión.
* Al reportar el estado del proyecto.
* Al decidir si las tareas activas reflejan la realidad.

## Estructura recomendada

```text
05_progress.md
05_progress/
├── current.md
├── by-component/
│   ├── frontend.md
│   ├── backend.md
│   ├── database.md
│   └── infrastructure.md
└── archive/
    └── YYYY-MM.md
```

## Esquema de progreso actual

```markdown
# 05 — Progreso Actual

> Última actualización: {YYYY-MM-DD HH:MM zona horaria}
> Fase activa: {nombre de la fase}

## Resumen ejecutivo

{estado actual breve y próxima prioridad}

## Completado

| Item | Resultado | Evidencia |
|------|-----------|----------|
| {componente/tarea} | {qué cambió} | {prueba, PR, commit, enlace o nota} |

## En progreso

| Item | Responsable | Estado | Siguiente paso |
|------|-------------|--------|----------------|
| {tarea} | {persona/agente} | {estado} | {próxima acción concreta} |

## Pendiente

| Prioridad | Item | Razón |
|-----------|------|-------|
| {n} | {trabajo} | {por qué importa} |

## Riesgos y bloqueos

| Riesgo | Impacto | Mitigación |
|--------|---------|------------|
| {riesgo} | {impacto} | {próxima acción} |
```

## Esquema de progreso por componente

```markdown
# Progreso — {Componente}

> Última actualización: {YYYY-MM-DD}

## Estado actual

{dónde está este componente}

## Trabajo completado

* {item fechado con evidencia}

## Trabajo pendiente

* {item pendiente enlazado a tarea cuando sea accionable}

## Notas para agentes

* {contexto específico del componente}
```

## Checklist

* `current.md` es lo suficientemente corto para leerlo al inicio de la sesión.
* Los items completados incluyen evidencia o validación.
* Los items pendientes enlazan a tareas cuando son accionables.
* El detalle de estado antiguo se archiva mensualmente.
* El progreso no duplica los archivos de tarea línea por línea.
* Si `current.md` supera 12 000 caracteres y el proyecto tiene el MCP `iaWorkflow`, usar `archive_progress` (parámetro `keepDays`, default 7) para mover entradas antiguas de `## Completado en sesiones recientes` a `05_progress/archive/YYYY-MM.md`. Requiere `apply: true` explícito; es idempotente.

## Estructura recomendada

```text
05_progress.md
05_progress/
├── current.md
├── by-component/
│   ├── frontend.md
│   ├── backend.md
│   ├── database.md
│   └── infrastructure.md
└── archive/
    └── YYYY-MM.md
```

## Esquema de progreso actual

```markdown
# 05 — Progreso Actual

> Última actualización: {YYYY-MM-DD HH:MM zona horaria}
> Fase activa: {nombre de la fase}

## Resumen ejecutivo

{estado actual breve y próxima prioridad}

## Completado

| Item | Resultado | Evidencia |
|------|-----------|----------|
| {componente/tarea} | {qué cambió} | {prueba, PR, commit, enlace o nota} |

## En progreso

| Item | Responsable | Estado | Siguiente paso |
|------|-------------|--------|----------------|
| {tarea} | {persona/agente} | {estado} | {próxima acción concreta} |

## Pendiente

| Prioridad | Item | Razón |
|-----------|------|-------|
| {n} | {trabajo} | {por qué importa} |

## Riesgos y bloqueos

| Riesgo | Impacto | Mitigación |
|--------|---------|------------|
| {riesgo} | {impacto} | {próxima acción} |
```

## Esquema de progreso por componente

```markdown
# Progreso — {Componente}

> Última actualización: {YYYY-MM-DD}

## Estado actual

{dónde está este componente}

## Trabajo completado

* {item fechado con evidencia}

## Trabajo pendiente

* {item pendiente enlazado a tarea cuando sea accionable}

## Notas para agentes

* {contexto específico del componente}
```

## Checklist

* `current.md` es lo suficientemente corto para leerlo al inicio de la sesión.
* Los items completados incluyen evidencia o validación.
* Los items pendientes enlazan a tareas cuando son accionables.
* El detalle de estado antiguo se archiva mensualmente.
* El progreso no duplica los archivos de tarea línea por línea.

## Errores comunes

* Convertir el progreso en un diario histórico largo.
* Marcar trabajo como completo sin evidencia de validación.
* Mantener bloqueos obsoletos después de que se resolvieron.
* Mezclar ideas futuras con trabajo pendiente activo.
