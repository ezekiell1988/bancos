---
title: IA Workflow Assistant Instructions Reference
description: Referencia y checklist para crear o actualizar instrucciones de GitHub Copilot, Claude y Codex en proyectos que usan /ia e iaWorkflow.
---

## Propósito

Los archivos de instrucciones enseñan a cada asistente cómo operar el workflow del proyecto. `/ia` conserva la fuente de verdad y `iaWorkflow` es la interfaz operativa cuando el MCP está habilitado.

Al inicializar o actualizar `/ia`, crear o actualizar las instrucciones de GitHub Copilot, Claude y Codex. Deben compartir el mismo contrato y conservar las reglas específicas que ya tenga el proyecto.

## Cuándo leer

* Al inicializar `/ia` en un proyecto nuevo.
* Al crear o configurar el MCP `iaWorkflow`.
* Al auditar instrucciones de asistentes existentes.
* Al cambiar el contrato de contexto, tareas, revisión o cierre.

## Conjunto recomendado de archivos

| Asistente | Archivo | Propósito |
|---|---|---|
| GitHub Copilot | `.github/copilot-instructions.md` | Instrucciones globales de Copilot en VS Code |
| Instrucciones con alcance de Copilot | `.github/instructions/*.instructions.md` | Reglas opcionales por patrón de archivos |
| Claude | `CLAUDE.md` | Instrucciones del proyecto para Claude |
| Codex | `AGENTS.md` | Instrucciones para Codex y agentes compatibles |

Crear los tres archivos principales cuando el proyecto soporte esos asistentes. Si alguno ya existe, actualizarlo sin reemplazar sus reglas específicas.

## Contrato compartido cuando existe iaWorkflow

Si el proyecto instala y expone el MCP `iaWorkflow`, cada archivo principal debe indicar de forma explícita:

* `/ia` es la fuente de verdad del contexto, pero `iaWorkflow` es su interfaz operativa obligatoria.
* Para cualquier tarea, pregunta, planificación, diagnóstico, revisión, implementación o cierre, iniciar con `ia_validate` y `ia_get_context` usando la intención adecuada.
* Para trabajar sobre una tarea, usar `work_task` antes de editar; solo implementar tareas `Lista` y exigir aprobación explícita cuando el riesgo sea alto.
* Al finalizar, usar `finish_task`; usar las acciones MCP correspondientes para crear, aprobar, bloquear, inspeccionar o cerrar elementos del workflow.
* Usar `ia_inspect` solo para lecturas puntuales que el contexto MCP enrute o que sean necesarias para el trabajo actual.
* No leer directamente `ia/README.md` ni recorrer `/ia` manualmente para reconstruir contexto. `ia/README.md` sigue siendo documentación y navegación humana.
* Usar los skills de gestión de tareas, revisión de código y cierre de sesión definidos por el proyecto.
* No exponer secretos, tokens, contraseñas, cadenas de conexión ni claves privadas.

No escribir instrucciones que mezclen este contrato con “empezar leyendo `ia/README.md`” cuando `iaWorkflow` está disponible.

## Fallback sin MCP

Solo si el proyecto no tiene `iaWorkflow` disponible, las instrucciones pueden usar `ia/README.md` como punto de entrada para el LLM y explicar sus reglas de lectura. Al instalar el MCP, reemplazar ese fallback por el contrato anterior.

## Plantilla mínima

Adaptar esta plantilla al idioma y los skills reales del proyecto:

```markdown
## Workflow obligatorio

Para cualquier tarea, consulta, planificación, revisión o diagnóstico usa primero el MCP `iaWorkflow`. `/ia` es la fuente de verdad, pero el MCP es su interfaz operativa; no leas directamente `ia/README.md` ni recorras `/ia` manualmente para obtener contexto.

- Al iniciar, usa `ia_validate` y `ia_get_context` con la intención adecuada.
- Para una tarea concreta, usa `work_task` antes de editar y `finish_task` al cerrar.
- Usa `ia_inspect` solo para lecturas puntuales enrutadas por el contexto MCP.
- Aplica los skills de gestión de tareas, revisión y cierre definidos por el proyecto.
```

## Guía por asistente

### GitHub Copilot

Usar `.github/copilot-instructions.md` como instrucción global. Mantenerla breve y escribir el contrato MCP. Usar `.github/instructions/*.instructions.md` únicamente para reglas por patrón; cada una necesita frontmatter `description` y `applyTo`.

```yaml
---
description: "Instrucciones específicas del proyecto para archivos de backend"
applyTo: "src/backend/**"
---
```

### Claude

Usar `CLAUDE.md` para el contrato MCP, los skills de workflow, reglas de seguridad y expectativas de validación. No duplicar los esquemas de `/ia`.

### Codex

Usar `AGENTS.md` con reglas operativas explícitas: validación y contexto por MCP, ciclo de vida de tareas, revisión, cierre y manejo de secretos. No requerir que el agente lea directamente `ia/README.md` cuando el MCP esté configurado.

## Reglas de ubicación y contenido

* Mantener `.github/copilot-instructions.md` y `.github/instructions/` bajo `.github/`.
* Mantener `CLAUDE.md` y `AGENTS.md` en la raíz salvo razón documentada.
* No copiar esquemas completos de `/ia` ni reglas extensas de ciclo de vida en cada archivo; referir a los skills y al MCP.
* Mantener los archivos de instrucciones específicos del proyecto y esta referencia genérica.
* No incluir secretos ni valores de entorno.

## Checklist de generación y auditoría

* Los tres archivos principales existen o la auditoría explica su ausencia.
* Cuando existe `iaWorkflow`, los tres exigen `ia_validate` e `ia_get_context` antes de trabajar.
* Cuando existe `iaWorkflow`, ninguno indica abrir directamente `ia/README.md` ni recorrer `/ia` para obtener contexto.
* `work_task` y `finish_task` aparecen en las instrucciones cuando el MCP los expone.
* Cada asistente conoce los skills de gestión de tareas, revisión y cierre del proyecto.
* Las instrucciones no contienen secretos ni duplican esquemas de `ia/SCHEMAS.md`.
* Las instrucciones por patrón de Copilot tienen frontmatter válido.

## Errores comunes

* Mantener la regla de `ia/README.md` después de habilitar `iaWorkflow`.
* Hacer que Copilot, Claude y Codex usen contratos distintos de contexto.
* Exponer al usuario las tools de bajo nivel como si fueran el flujo principal.
* Reemplazar instrucciones existentes sin conservar sus reglas de proyecto.
