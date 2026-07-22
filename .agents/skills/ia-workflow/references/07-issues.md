---
title: IA Workflow 07 Issues Example
description: Ejemplo y checklist para 07_issues.md y 07_issues/ en un workflow /ia genérico.
---

## Propósito

`07_issues.md` y `07_issues/` capturan bugs conocidos, limitaciones y comportamientos inesperados. Preservan el contexto de reproducción y las correcciones para que los agentes no redescubran el mismo problema.

## Cuándo leer

* Al depurar un fallo.
* Al revisar un cambio que toca comportamiento frágil.
* Al cerrar o archivar issues resueltos.
* Al verificar si un nuevo síntoma ya tiene historial.

## Estructura recomendada

```text
07_issues.md
07_issues/
├── current.md
├── open/
│   └── ISSUE-001-ejemplo.md
└── archive/
    └── YYYY-MM.md
```

Usar `07_issues/current.md` como índice activo conciso y un archivo por issue activo en `07_issues/open/`.

## Esquema de issue

```markdown
## ISSUE-{NNN} — {título}

**Severidad:** Crítica | Alta | Media | Baja
**Estado:** Pendiente (`Pending`) | Investigando (`Investigating`) | Resuelto (`Resolved`)
**Detectado:** {YYYY-MM-DD}
**Última actualización:** {YYYY-MM-DD HH:MM zona horaria}
**Reportado por:** {persona o sistema}
**Componente:** {módulo/ruta/área del sistema}
**Tarea relacionada:** {TASK-ID o ninguna}

### Síntoma

{comportamiento observable del usuario, logs o pruebas}

### Impacto

{quién se ve afectado y cuán grave es}

### Reproducción

1. {paso}
2. {paso}
3. {resultado esperado vs actual}

### Causa raíz

{causa conocida o "desconocida" mientras se investiga}

### Workaround

{acción temporal o ninguna}

### Corrección propuesta o aplicada

{detalles del fix, PR, commit o enlace a tarea}

### Aprendizaje

{regla para prevenir la recurrencia}
```

## Ciclo de vida

1. Agregar issues sin resolver a `07_issues/current.md` y, cuando el detalle sea útil, a `07_issues/open/ISSUE-ID.md`.
2. Enlazar tareas relacionadas cuando se requiere trabajo.
3. Actualizar causa raíz y workaround a medida que mejora la evidencia.
4. Mover el detalle del issue resuelto a `07_issues/archive/YYYY-MM.md` y eliminar el archivo activo individual de `07_issues/open/`.
5. Mantener un índice conciso en `07_issues.md`.

## Checklist

* Cada issue tiene un síntoma y un impacto.
* Los pasos de reproducción son concretos o el issue está marcado como no reproducible aún.
* La causa raíz está separada del síntoma.
* Los detalles del fix no exponen secretos de los logs.
* Los issues resueltos se archivan sin perder el aprendizaje.

## Errores comunes

* Crear issues para funcionalidades planificadas.
* Ocultar la causa raíz desconocida detrás de especulación.
* Pegar logs sensibles o tokens.
* Dejar issues resueltos en el archivo activo.
