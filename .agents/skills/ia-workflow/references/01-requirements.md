---
title: IA Workflow 01 Requirements Example
description: Ejemplo y checklist para 01_requirements.md en un workflow /ia genérico.
---

## Propósito

`01_requirements.md` describe qué debe hacer el sistema desde la perspectiva del negocio. Registra comportamientos, reglas y resultados visibles para el usuario antes de los detalles de implementación.

## Cuándo leer

* Al planificar una nueva funcionalidad o flujo de trabajo.
* Al revisar si un cambio de código preserva el comportamiento del negocio.
* Al depurar un comportamiento que puede ser una violación de reglas.

## Pertenece a

* Propósito del negocio
* Requisitos funcionales
* Invariantes del negocio
* Roles o personas de usuario
* Flujos principales y criterios de éxito
* Restricciones no funcionales que afectan el comportamiento del producto

## No pertenece a

* Arquitectura técnica. Usar `02_architecture.md`.
* Tareas de implementación. Usar `04_tasks/`.
* Detalles de despliegue. Usar `02_architecture.md` o un documento específico de infraestructura.

## Esquema recomendado

```markdown
# 01 — Requisitos del Sistema

> Última actualización: {YYYY-MM-DD}
> Fuentes: {tickets, notas de stakeholders, documentos, enlaces}

## Propósito del sistema

{una o dos oraciones describiendo el resultado de negocio}

## Roles de usuario

| Rol | Objetivo | Permisos o restricciones |
|-----|----------|-------------------------|
| {rol} | {objetivo} | {límites} |

## Invariantes del negocio

* {regla que siempre debe cumplirse}
* {regla que no debe romperse con futuros cambios}

## Requisitos funcionales

| ID | Requisito | Señal de aceptación |
|----|-----------|---------------------|
| REQ-001 | {el sistema debe...} | {resultado observable} |

## Flujos principales

### Flujo: {nombre}

Estado inicial: {estado}
Estado final: {estado}

1. {acción del actor o paso del sistema}
2. {acción del actor o paso del sistema}
3. {salida observable}

## Fuera de alcance

* {exclusión explícita}
```

## Checklist

* Los requisitos se expresan como comportamientos observables.
* Cada requisito puede verificarse con una prueba, revisión o acción del usuario.
* Las reglas del negocio no dependen de nombres de clases internas o tablas.
* Las ambigüedades están marcadas como preguntas abiertas en vez de adivinadas.
* Los elementos fuera de alcance son lo suficientemente claros para prevenir expansión accidental.

## Errores comunes

* Describir diseño de código en lugar de comportamiento del negocio.
* Mezclar decisiones de stakeholders con tareas de implementación.
* Registrar deseos sin señales de aceptación.
* Mantener incertidumbre resuelta como prosa vaga en vez de actualizar la regla.
