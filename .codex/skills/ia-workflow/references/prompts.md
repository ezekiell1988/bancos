---
title: IA Workflow Prompts Reference
description: Referencia y checklist para crear los prompts reutilizables que impulsan el workflow /ia.
---

## Propósito

Los prompts reutilizables convierten el workflow `/ia` en comandos repetibles. Cada prompt carga los archivos `/ia` correctos, declara un objetivo, establece reglas y define la salida esperada, para que un agente ejecute un momento estándar del workflow de la misma manera cada vez.

Crear un conjunto de prompts al inicializar `/ia` para que el workflow sea impulsado por comandos, no por instrucciones ad hoc.

## Cuándo leer

* Al inicializar el workflow `/ia` en un proyecto nuevo.
* Al auditar si cada momento del workflow tiene un prompt.
* Al adaptar prompts tras cambios en la estructura de `/ia` o los skills de workflow.

## Pertenece a

* El comando reutilizable para cada momento del workflow.
* La lista de archivos `/ia` que cada comando debe leer.

## No pertenece a

* Reglas del ciclo de vida. Esas viven en los skills de workflow.
* Estructura de artefactos. Esa vive en `ia/templates/`.

## Dónde viven los prompts

* Los prompts invocables por agentes usualmente viven en `.github/prompts/` como `*.prompt.md`.
* Un índice corto puede vivir en `ia/prompts/README.md` para que `/ia` documente los comandos disponibles.

## Conjunto recomendado de prompts

| Prompt | Momento del workflow | Skill principal |
|--------|---------------------|----------------|
| `create-spec` | Convertir una necesidad en requisitos o especificación | `01_requirements.md` |
| `create-task` | Convertir un requisito en una tarea accionable | gestión de tareas |
| `implement-next-task` | Implementar la tarea seleccionada | skills de área |
| `review-current-diff` | Revisar el conjunto de cambios | revisión de código |
| `debug-known-issue` | Trabajar un issue registrado | issues |
| `close-session` | Persistir el estado de vuelta en `/ia` | cierre de sesión |
| `create-skill-from-retrospective` | Convertir un aprendizaje en un skill | retrospectiva |

## Estructura del prompt

* Frontmatter con una `description` de una sola línea.
* Una lista `Leer` con los archivos `/ia` exactos que el agente debe cargar.
* Una declaración `Objetivo`.
* `Reglas` que restringen la ejecución, incluyendo qué no hacer.
* Una sección `Salida esperada`.

## Checklist

* Cada momento del workflow tiene un prompt.
* Cada prompt lee solo los archivos `/ia` que necesita.
* Los prompts apuntan a templates y skills en vez de duplicar su contenido.
* Los prompts se mantienen neutros respecto al proyecto; los hechos del proyecto vienen de los archivos `/ia` que leen.

## Errores comunes

* Un prompt que codifica reglas ya pertenecientes a un skill de workflow.
* Un prompt que carga toda la carpeta `/ia` en vez de los archivos que necesita.
* Prompts faltantes para revisión o cierre de sesión, haciendo que esos momentos se omitan.
