---
name: ia-nueva-tarea
description: >
  Guía para crear una nueva tarea TASK-{INICIALES}-{ÁREA}-{NN} en el sistema /ia del proyecto voice-bot.
  Usar cuando se pida registrar una nueva tarea, planificar un feature, documentar trabajo nuevo, o 
  convertir un item del backlog en tarea accionable. Genera el archivo individual en `ia/04_tasks/tasks/`
  y actualiza `ia/04_tasks/current.md`. Por defecto crea tareas en Borrador; aprobarlas requiere validación explícita.
  Triggers: nueva tarea, crear task, agregar task, /ia-nueva-tarea, documentar trabajo, crear TASK.
---

# Crear Nueva Tarea en /ia

Crea tareas accionables siguiendo el estándar del proyecto. Una tarea bien definida permite que un agente la implemente leyendo solo su archivo.

---

## Cuándo usar

- Se identifica un feature, fix o investigación que debe hacerse
- Se quiere convertir un item del backlog en tarea implementable
- Se empieza una sesión nueva y la tarea aún no está documentada
- Se necesita aprobar una tarea `Borrador` después de validar su contrato

## No usar cuando

- La tarea ya existe en `ia/04_tasks/tasks/`
- Es solo una nota o idea — en ese caso, agregar a `ia/04_tasks/backlog.md`
- El usuario pide implementar una tarea ya `Lista` — usar el skill técnico del área

---

## Contexto requerido

Leer antes de crear:

- `ia/00_context.md` — stack y constantes del proyecto
- `ia/04_tasks/current.md` — para ver el siguiente número secuencial disponible por área
- `ia/04_tasks/backlog.md` — si viene de ahí

---

## Nomenclatura de IDs

```
TASK-{INICIALES}-{ÁREA}-{NN}
```

| Segmento | Regla |
|---|---|
| `INICIALES` | Primera letra mayúscula de cada palabra del nombre del autor, máx. 3. `Ezequiel Baltodano Cubillo` → `EBC` |
| `ÁREA` | Código funcional (tabla abajo) |
| `NN` | Secuencial de 2 dígitos por persona+área, iniciando en 01 |

### Catálogo de áreas

| Código | Alcance |
|---|---|
| `FE` | Frontend — Angular / Ionic (`VoiceBot.Web`, `VoiceBot.App`) |
| `BE` | Backend — API .NET (`VoiceBot.Api`) |
| `DB` | Base de datos — SQL, migraciones EF |
| `INF` | Infraestructura — Azure, CI/CD, pipelines |
| `DOC` | Documentación, archivos `/ia`, `/docs` |
| `MCP` | MCP local `.mcp/ia-workflow`, tools, prompts y orquestación |
| `QA` | Testing, E2E, auditoría visual |

---

## Estados, riesgo y aprobación

Estados canónicos:

| Estado | Uso |
|---|---|
| `Borrador` | Tarea creada, pendiente de aprobación. No implementar. |
| `Lista` | Contrato completo y aprobado. Se puede implementar. |
| `En progreso` | Trabajo activo. |
| `Bloqueada` | Requiere desbloqueo externo. |
| `En revisión` | Implementada, pendiente de validar/revisar. |
| `Completada` | Archivada en `done/YYYY-MM.md`. |

Toda tarea nueva debe incluir:

- `Riesgo: bajo | medio | alto`
- `Aprobación: pendiente | aprobada | no requerida`

Riesgo alto incluye login, seguridad, permisos, pagos, base de datos, infraestructura o cambios destructivos. Requiere aprobación explícita del usuario antes de implementar.

## Procedimiento

1. **Determinar el ID** — revisar `ia/04_tasks/current.md` y `ia/04_tasks/tasks/` para saber qué número sigue por área.

2. **Crear el archivo** en `ia/04_tasks/tasks/TASK-{ID}.md` usando la plantilla:

```
ia/templates/task-template.md
```

Campos obligatorios: `Estado`, `Autor`, `Rama`, `Fecha inicio`, `Área`, `Prioridad`, `Riesgo`, `Aprobación`, `Title`, `Context`, `Objetivo`, `Alcance permitido`, `Fuera de alcance`, `Criterios de aceptación`, `Steps`, `Expected Output`, `Validación`, `Rollback`.

Por defecto, crear con `Estado: Borrador` y `Aprobación: pendiente`, salvo que el usuario apruebe explícitamente la implementación y el riesgo no exija una segunda confirmación.

3. **Actualizar `ia/04_tasks/current.md`** — agregar la fila en "Borradores" o "Próximas" según el estado.

4. Si viene de backlog: **eliminar o tachar** el item en `ia/04_tasks/backlog.md`.

5. Para aprobar una tarea existente: verificar campos obligatorios, marcar `Estado: Lista`, `Aprobación: aprobada` si aplica, y mover la fila desde "Borradores" hacia "Próximas".

---

## Safety rules

- No crear una tarea sin al menos `Title`, `Context` y `Steps` verificables.
- `Expected Output` debe ser verificable — si no se puede verificar, la tarea está mal definida.
- No implementar tareas en `Borrador`.
- No aprobar tareas de riesgo alto sin confirmación explícita del usuario.
- No modificar IDs existentes — son inmutables.
- No exponer connection strings, tokens ni passwords en ningún campo.

---

## Expected output

- `ia/04_tasks/tasks/TASK-{ID}.md` creado con todos los campos obligatorios
- `ia/04_tasks/current.md` actualizado con la nueva fila en la cola activa
