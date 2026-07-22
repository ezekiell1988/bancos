---
title: IA Workflow 06 Decisions Example
description: Referencia y checklist para 06_decisions.md y 06_decisions/ en un workflow /ia.
---

## Propósito

`06_decisions.md` y `06_decisions/` almacenan Architecture Decision Records (ADRs). Los ADRs explican por qué se tomaron decisiones importantes para que los agentes no las reviertan accidentalmente.

La guía de esta referencia usa español; el ADR y su índice generados deben escribirse en el idioma
del proyecto, salvo nombres de código, comandos, identificadores externos o citas literales.

## Cuándo leer

* Cambiar arquitectura, límites, persistencia, autenticación o despliegue.
* Revisar si un cambio contradice decisiones previas.
* Explicar por qué existe un patrón antiguo.

## Estructura recomendada

```text
06_decisions.md
06_decisions/
├── ADR-01-example-decision.md
├── ADR-02-example-decision.md
└── ADR-03-example-decision.md
```

`06_decisions.md` es solo un índice liviano. Cada ADR vive en un archivo individual `06_decisions/{ADR-ID}-{slug}.md`. No agrupar ADRs por dominio en archivos como `frontend.md` o `backend.md`, porque vuelven a crecer y obligan a los agentes a cargar contexto no relacionado.

## Esquema ADR

```markdown
## ADR-{NN}: {Título}

**Estado:** Propuesta | Aceptada | Reemplazada
**Fecha:** {YYYY-MM-DD}
**Dominio:** {frontend | backend | database | infraestructura | agentic | otro}
**Reemplaza:** {ADR-ID o ninguno}

### Contexto

{problema, restricción o fuerza que requirió una decisión}

### Decisión

{qué decidió el equipo}

### Razón

{por qué esta es la mejor opción bajo las restricciones}

### Alternativas descartadas

| Alternativa | Razón de descarte |
|-------------|-------------------|
| {opción} | {razón} |

### Consecuencias

* {consecuencia positiva o negativa}
* {seguimiento requerido}
```

## Esquema de índice

```markdown
# 06 — Decisiones Arquitectónicas (ADRs)

> Los ADRs son append-only. No borrar ADRs históricos.
> Este archivo es solo índice. El detalle vive en `06_decisions/{ADR-ID}-{slug}.md`.
> Idioma: {idioma_del_proyecto}.

| ADR | Título | Estado | Dominio | Fecha | Archivo |
|-----|--------|--------|---------|-------|---------|
| ADR-01 | {título} | Aceptada | backend | {YYYY-MM-DD} | `06_decisions/ADR-01-{slug}.md` |
```

## Rules

* Nunca borrar un ADR.
* Reemplazar decisiones antiguas con un ADR nuevo en vez de reescribir la historia.
* Registrar decisiones antes de implementar cambios que alteren arquitectura.
* Mantener detalles de implementación cortos y enlazar tareas para la ejecución.
* Mantener `06_decisions.md` como índice; el contexto completo va en el archivo individual del ADR.
* Escribir ADRs en el idioma del proyecto.

## Checklist

* Cada cambio arquitectónico importante tiene un ADR.
* La decisión explica tradeoffs, no solo el resultado final.
* Las decisiones reemplazadas apuntan al ADR nuevo.
* Cada ADR vive en su propio archivo.
* El índice enlaza todos los ADRs existentes.

## Errores comunes

* Tratar ADRs como minutas de reunión.
* Cambiar ADRs antiguos para que coincidan con la realidad actual.
* Registrar preferencias sin razón.
* Usar ADRs para tareas ordinarias de implementación.
* Volver a agrupar ADRs en archivos grandes por dominio.
