---
title: IA Workflow 08 Retrospective Example
description: Ejemplo y checklist para 08_retrospective.md en un workflow /ia genérico.
---

## Propósito

`08_retrospective.md` captura aprendizajes de fases completadas o sesiones significativas. Convierte errores repetidos y patrones exitosos en mejoras de proceso futuras.

## Cuándo leer

* Al cerrar una fase, sprint o grupo grande de tareas.
* Al crear o mejorar un skill a partir de lecciones repetidas.
* Al revisar cómo el equipo debería ajustar su flujo de trabajo.

## Pertenece a

* Qué funcionó
* Qué ralentizó al equipo
* Decisiones que deberían cambiar la próxima vez
* Skills, templates o checks candidatos a agregar
* Aprendizajes de proceso más amplios que una sola tarea

## No pertenece a

* Estado activo de tareas. Usar `04_tasks/current.md`.
* Estado actual del proyecto. Usar `05_progress/current.md`.
* Seguimiento de bugs. Usar `07_issues/`.
* Decisiones formales de arquitectura. Usar `06_decisions/`.

## Esquema recomendado

```markdown
# 08 — Retrospectiva

## Fase {N} — {nombre} ({fecha de cierre})

### Qué funcionó

* {práctica, patrón o decisión que ayudó}

### Qué debe mejorar

* {problema y ajuste concreto}

### Decisiones a revisar

* {decisión} -> {ajuste futuro recomendado}

### Skills o templates a crear

| Necesidad | Artefacto propuesto | Razón |
|-----------|---------------------|-------|
| {patrón repetido} | {skill/template/checklist} | {beneficio} |

### Acciones de seguimiento

| Acción | Responsable | Objetivo |
|--------|-------------|----------|
| {acción} | {responsable} | {fecha o fase} |
```

## Checklist

* Cada aprendizaje es lo suficientemente específico para cambiar el comportamiento futuro.
* Los errores técnicos repetidos se convierten en skills o checks candidatos.
* Los items de acción tienen responsables o se mueven a tareas cuando son accionables.
* La retrospectiva no duplica detalles de issues.
* El archivo se mantiene conciso agrupando entradas por fase o mes.

## Errores comunes

* Escribir elogios o quejas vagas sin próxima acción.
* Usar la retrospectiva como depósito de bugs no resueltos.
* Olvidar convertir lecciones repetidas en skills o templates.
* Mantener items de acción aquí en vez de crear tareas cuando se vuelven accionables.
