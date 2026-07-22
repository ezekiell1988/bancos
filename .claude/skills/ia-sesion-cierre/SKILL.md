---
name: ia-sesion-cierre
description: >
  Guía para cerrar una sesión de desarrollo en el proyecto actual: actualizar el archivo de tarea,
  current.md de 04_tasks, current.md de 05_progress, y registrar issues o ADRs si aplica.
  Usar al finalizar trabajo de una sesión, marcar una tarea como completada, o actualizar el estado
  del proyecto antes de cerrar el IDE.
  Triggers: cerrar sesión, fin de sesión, marcar completado, actualizar progreso, /ia-sesion-cierre,
  sesión terminada, commit y cierre.
---

# Cerrar Sesión de Desarrollo

Asegura que el estado del proyecto quede consistente al terminar una sesión.

---

## Cuándo usar

- Al finalizar trabajo en una tarea (parcial o completamente)
- Antes de hacer commit y push
- Al final de la jornada de desarrollo

## No usar cuando

- Solo se hizo investigación sin cambios en código (puede aplicar igual si hay hallazgos)

---

## Contexto requerido

Leer antes de actualizar:

- `ia/04_tasks/tasks/TASK-{ID}.md` — la tarea en la que se trabajó
- `ia/04_tasks/current.md` — cola activa
- `ia/05_progress/current.md` — estado actual

---

## Procedimiento

### 1. Actualizar el archivo de tarea individual

En `ia/04_tasks/tasks/TASK-{ID}.md`:

- Actualizar `**Estado:**` con el nuevo estado (`🔄 En progreso` o `✅ Completado`)
- Estados válidos: `Borrador`, `Lista`, `En progreso`, `Bloqueada`, `En revisión`, `Completada`
- Si completada: agregar `**Fecha cierre:** YYYY-MM-DD HH:MM CR`
- Si queda pendiente de revisión humana o validación runtime: usar `En revisión`, no `Completada`
- En sección `## Notes`: documentar hallazgos importantes de la sesión
- En `## Expected Output`: marcar con `[x]` los ítems completados

### 2. Actualizar `ia/04_tasks/current.md`

- Si la tarea está **en progreso**: actualizar la fila en "En progreso" con la nota de avance
- Si la tarea está **en revisión**: mover o mantener la fila en "En revisión" con la nota pendiente
- Si la tarea está **bloqueada**: agregar o actualizar su entrada en `04_tasks/blocked.md`
- Si la tarea está **completada**:
  - **Eliminar la fila** de la cola activa en `current.md` — la entrada en `done/` es el historial; `current.md` solo debe contener tareas no terminadas
  - Agregar una fila en "En progreso" si hay otra tarea activa

### 3. Mover a done/ si está completada

Si la tarea quedó `✅ Completado`:
- Agregar una fila en `ia/04_tasks/done/YYYY-MM.md` con fecha, ID, resumen y autor
- **Eliminar** el archivo `ia/04_tasks/tasks/{TASK-ID}.md` — el historial oficial queda en `done/`

### 4. Actualizar `ia/05_progress/current.md`

- Mover la tarea completada de "Trabajo activo" a "Últimas tareas completadas"
- Ajustar "Pendiente inmediato" según el nuevo estado
- Actualizar la fecha de `**Última actualización:**`

### 5. Actualizar `ia/05_progress/by-component/{área}.md`

- Agregar una fila en "Entradas recientes" con fecha y descripción del cambio
- Actualizar "Estado actual" si cambió algo significativo

### 6. Si aplica: registrar ADR o Issue

- **ADR**: si se tomó una decisión arquitectónica durante la sesión → crear un archivo individual en `ia/06_decisions/{ADR-ID}-{slug}.md` usando `ia/templates/adr-template.md` y agregar/actualizar su fila en el índice `ia/06_decisions.md`
- **Issue**: si se encontró un bug no resuelto → agregar en `ia/07_issues.md`

---

## Formato de sufijo para entradas de progreso

```
· _YYYY-MM-DD HH:MM CR · Nombre Apellido <email>_
```

Ejemplo: `· _2026-06-15 14:30 CR · Nombre Apellido <correo@example.com>_`

---

## Safety rules

- Hora siempre en Costa Rica (UTC-6).
- No dejar `current.md` desincronizado con el estado real.
- No marcar como `Completada` una tarea con validaciones críticas pendientes; usar `En revisión`.
- Eliminar el archivo de tarea de `tasks/` una vez registrado en `done/` — el historial oficial vive en `done/`, no en `tasks/`.
- No borrar ADRs ni entradas de `done/` — son inmutables.
- Mantener todo el contenido de `/ia` en el idioma declarado por el proyecto.

---

## Expected output

- Archivo de tarea actualizado con estado y notas de sesión
- `ia/04_tasks/current.md` refleja el estado actual
- `ia/05_progress/current.md` refleja las últimas completadas y el trabajo activo
- `ia/05_progress/by-component/` actualizado para el área afectada
