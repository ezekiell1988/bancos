---
name: ia-planificar
description: >
  Guía para iniciar una sesión de planificación en el proyecto voice-bot: qué leer, cómo evaluar
  prioridades, cómo proponer la siguiente tarea a implementar, y cómo actualizar el plan si cambió
  el contexto. Usar cuando se empiece una sesión sin tarea definida, se quiera replantear prioridades,
  o se necesite decidir qué hacer a continuación.
  Triggers: qué hacemos hoy, próxima tarea, planificar, priorizar, /ia-planificar, qué sigue,
  inicio de sesión, planning, ¿por dónde empezamos?.
---

# Planificar — Inicio de Sesión

Guía para orientarse rápido y decidir qué implementar en una sesión de desarrollo.

---

## Cuándo usar

- Al inicio de una sesión sin tarea asignada
- Cuando cambió el contexto (nueva sesión con Yafit, bug crítico encontrado, cambio de prioridad)
- Para replantear el backlog y cola activa

## No usar cuando

- Ya hay una tarea en progreso clara — ir directo a implementar

---

## Lectura mínima para planificar

En este orden:

1. `ia/00_context.md` — stack y constantes (si no está fresco en contexto)
2. `ia/04_tasks/current.md` — cola activa y qué está en progreso
3. `ia/04_tasks/blocked.md` — verificar si algo se desbloqueó
4. `ia/05_progress/current.md` — estado ejecutivo y riesgos abiertos
5. `ia/07_issues.md` — issues críticos que puedan cambiar la prioridad

**No leer por defecto:** `ia/04_tasks/done/`, `ia/05_progress/archive/`, `ia/04_tasks/tasks/` individuales (solo los de la tarea elegida).

---

## Procedimiento

### 1. Verificar el estado actual

- ¿Hay alguna tarea `En progreso`? → **completarla primero** antes de tomar otra.
- ¿Hay alguna tarea `Borrador`? → no implementarla; validar si debe aprobarse o quedarse pendiente.
- ¿Hay alguna tarea `En revisión`? → resolver validación/revisión antes de tomar trabajo nuevo.
- ¿Hay algún issue `🔴 Crítico` abierto? → puede override la prioridad.
- ¿Hay algún bloqueo en `blocked.md` que ya se desbloqueó? → moverlo a `current.md`.

### 2. Elegir la próxima tarea

Criterios en orden de prioridad:

1. Tarea en progreso (siempre primero)
2. Tarea en revisión que solo requiere validar/cerrar
3. Issue crítico bloqueante para producción
4. Tarea `Lista` con `Prioridad: Alta` en `current.md`
5. Tarea que desbloquea otras (ver campo `Dependencies` en la tarea)
6. Tarea de BD si hay migraciones pendientes antes de un feature
7. Tarea de menor riesgo si el tiempo disponible es limitado

### 3. Leer el archivo de la tarea elegida

```
ia/04_tasks/tasks/TASK-{ID}.md
```

Verificar:
- Dependencias satisfechas
- Archivos relacionados existen en el repo
- Steps son claros y verificables
- Estado es `Lista` o `En progreso`; alias legacy `pendiente` se puede tratar como `Lista`
- Si `Riesgo: alto`, existe `Aprobación: aprobada` o confirmación explícita del usuario

### 4. Si la tarea no existe como archivo individual

Usar el skill `ia-nueva-tarea` para crearla primero.

### 5. Si hay que replantear prioridades

- Editar `ia/04_tasks/current.md` — reordenar la columna `Prioridad`
- Si un item del backlog se volvió urgente: convertirlo en tarea con `ia-nueva-tarea`
- Documentar el cambio de prioridad como nota en `ia/06_decisions.md` si es una decisión arquitectónica

---

## Output esperado de una sesión de planificación

- Una tarea identificada y lista para implementar
- `ia/04_tasks/current.md` refleja el orden correcto
- El agente tiene claro el archivo de tarea a leer antes de codificar

---

## Safety rules

- No implementar sin leer el archivo de la tarea completa.
- No implementar tareas en `Borrador`, `Bloqueada`, `En revisión` o `Completada`.
- No implementar tareas de riesgo alto sin aprobación explícita.
- No tomar más de una tarea por sesión.
- No ignorar dependencias no satisfechas.
- Si no hay tarea clara, crear una en `ia/04_tasks/backlog.md` y consultarlo con el usuario antes de proceder.
