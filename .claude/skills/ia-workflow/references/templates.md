---
title: IA Workflow Templates Reference
description: Referencia y checklist para crear la carpeta ia/templates usada por el workflow /ia.
---

## Propósito

`ia/templates/` contiene esqueletos de documentos reutilizables que mantienen consistentes las tareas, decisiones, issues y skills. Los skills y prompts de workflow llenan estos templates en vez de inventar estructura cada vez.

Crear esta carpeta al inicializar `/ia` para que cada artefacto recurrente tenga una forma estable.

## Cuándo leer

* Al inicializar el workflow `/ia` en un proyecto nuevo.
* Al auditar si los artefactos recurrentes comparten una única fuente de estructura.
* Al agregar un nuevo tipo de artefacto recurrente al workflow.

## Pertenece a

* El esqueleto y los campos requeridos de cada artefacto recurrente.
* El estilo de marcadores que indica qué debe completar el autor.

## No pertenece a

* Reglas del ciclo de vida. Esas viven en los skills de workflow.
* Contenido real. Los templates solo definen estructura.

## Conjunto recomendado de templates

| Template | Propósito | Llenado por |
|----------|----------|-------------|
| `task-template.md` | Esqueleto de tarea accionable con alcance y salida esperada | skill de gestión de tareas |
| `adr-template.md` | Registro de decisión arquitectónica | skill de cierre de sesión |
| `issue-template.md` | Bug o limitación conocida | skill de cierre de sesión |
| `skill-template.md` | Esqueleto de nuevo skill de agente | flujo de trabajo de retrospectiva |

## Guía de campos

* `task-template.md`: metadatos de cabecera (estado, autor, rama, fechas, área, prioridad, riesgo, aprobación), Contexto, Objetivo, Alcance con Incluye y No incluye, Criterios de aceptación, archivos probables, Plan técnico, Pasos, Comandos de validación, Rollback y Checklist.
* `adr-template.md`: ID, título, estado, fecha, contexto, decisión, alternativas, consecuencias.
* `issue-template.md`: ID, título, severidad, estado, reproducción, esperado vs actual, workaround, tarea relacionada.
* `skill-template.md`: frontmatter con `name` y `description`, Propósito, Cuándo usar, Procedimiento, Reglas, Salida esperada.

## Reglas de ubicación

* Mantener todos los templates bajo `ia/templates/`.
* Usar marcadores claros como `{título}` y `{YYYY-MM-DD}` para que los autores sepan qué reemplazar.
* Mantener los templates neutros respecto al proyecto. Las reglas específicas del proyecto pertenecen a los skills de workflow, no al esqueleto.

## Checklist

* Cada artefacto recurrente del workflow tiene un template.
* Cada template comienza con un encabezado de nivel 1 o frontmatter y usa marcadores.
* Los skills de workflow referencian templates por ruta en vez de duplicar estructura.
* Los templates de tarea distinguen el trabajo `Borrador` no aprobado del trabajo `Lista` aprobado.

## Errores comunes

* Embeber reglas del proyecto dentro de un template que debería mantenerse neutral.
* Skills que recrean estructura en vez de apuntar al template.
* Templates que se desfasan del archivo `/ia` que alimentan.
