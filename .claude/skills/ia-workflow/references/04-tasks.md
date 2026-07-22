---
title: IA Workflow 04 Tasks Example
description: Ejemplo y checklist para 04_tasks.md y 04_tasks/ en un workflow /ia genérico.
---

## Propósito

`04_tasks.md` y `04_tasks/` son el sistema operativo del trabajo accionable. Los agentes deben implementar una tarea a la vez desde esta área, con alcance claro y salida verificable.

En flujos de trabajo con delegación a agentes, cada tarea es el contrato de aprobación entre el humano y el agente. El agente solo puede implementar trabajo que esté explícitamente en alcance y aprobado.

## Cuándo leer

* Al crear una tarea.
* Al seleccionar trabajo para implementar.
* Al actualizar el estado de una tarea.
* Al bloquear o completar trabajo.

## Estructura recomendada

```text
04_tasks.md
04_tasks/
├── current.md
├── backlog.md
├── blocked.md
├── tasks/
│   └── TASK-ABC-FE-01.md
└── done/
    └── YYYY-MM.md
```

## Convención de ID

```text
TASK-{INICIALES}-{AREA}-{NN}
```

| Segmento | Regla | Ejemplo |
|----------|-------|---------|
| `INICIALES` | Iniciales en mayúsculas del `git config user.name`, máximo 3 letras | `ABC` |
| `AREA` | Código funcional fijo | `FE` |
| `NN` | Secuencia de dos dígitos por autor y área; al superar `99`, ampliar a tres dígitos sin reiniciar | `01`, `100` |

## Catálogo de áreas

| Código | Alcance |
|--------|---------|
| `FE` | Frontend |
| `BE` | Backend API o servicios |
| `HF` | Jobs en segundo plano, workers o schedulers |
| `DB` | Base de datos, migraciones, SQL o modelo de datos |
| `INF` | Infraestructura, despliegue o pipelines |
| `DOC` | Documentación y artefactos de conocimiento |
| `MCP` | Servidores o herramientas Model Context Protocol |
| `ARCH` | Decisiones de arquitectura transversales |
| `QA` | Pruebas, validación y auditorías |
| `CAP` | Capacitación, demos e incorporación |

## Estados del ciclo de vida

| Alias en inglés | Etiqueta en español | Significado |
|-----------------|---------------------|-------------|
| `Draft` | `Borrador` | Creada pero no aprobada para implementación. |
| `Ready` | `Lista` | Validada y aprobada; el agente puede implementar. |
| `In Progress` | `En progreso` | En implementación activa. |
| `Blocked` | `Bloqueada` | No puede continuar hasta que se resuelva una condición externa. |
| `Review` | `En revisión` | Implementación terminada, pendiente de revisión o validación. |
| `Done` | `Completada` | Terminada y archivada en el historial mensual. |

Solo las tareas `Lista` pueden seleccionarse para implementación. Los estados legados como `pendiente` pueden tratarse como `Lista` durante la migración, pero las tareas nuevas deben usar los estados canónicos.

## Niveles de riesgo

| Riesgo | Ejemplos | Regla |
|--------|----------|-------|
| `Bajo` | Texto, CSS, iconos, cambios solo de UI menores | La aprobación normal de tarea es suficiente. |
| `Medio` | Formularios, APIs simples, validaciones, estado de UI compartido | Verificar alcance y comandos de validación antes de implementar. |
| `Alto` | Login, seguridad, permisos, pagos, base de datos, infraestructura, cambios destructivos | Requiere aprobación explícita del usuario antes de implementar. |

## Template de tarea

```markdown
# TASK-{INICIALES}-{AREA}-{NN} — {título corto}

**Estado:** Borrador | Lista | En progreso | Bloqueada | En revisión | Completada
**Autor:** {git config user.name} `<{git config user.email}>`
**Rama:** {feature/iniciales/descripcion-kebab o -}
**Inicio:** {YYYY-MM-DD HH:MM zona horaria}
**Cierre:** {YYYY-MM-DD HH:MM zona horaria o -}
**Área:** {FE | BE | HF | DB | INF | DOC | MCP | ARCH | QA | CAP}
**Prioridad:** Alta | Media | Baja
**Riesgo:** Bajo | Medio | Alto
**Aprobación:** Pendiente | Aprobada | No requerida

---

## Título

{título expandido}

## Contexto

{por qué existe esta tarea}

## Objetivo

{una oración describiendo el resultado}

## Alcance

### Incluye

* {trabajo incluido}

### Excluye

* {trabajo explícitamente excluido}

## Criterios de aceptación

* [ ] {comportamiento verificable}
* [ ] {salida observable}

## Archivos probables

* `{ruta o pendiente de descubrir}`

## Plan técnico

1. {paso técnico}
2. {paso técnico}

## Pasos

1. {paso}
2. {paso}

## Salida esperada

{qué debe ser verdad cuando la tarea esté completa}

## Validación

* [ ] {resultado verificable}
* [ ] {comando de validación o verificación manual}

## Rollback

{cómo revertir de forma segura si esta tarea causa problemas}

## Checklist

* [ ] Alcance aprobado
* [ ] Riesgo revisado
* [ ] Implementación completa
* [ ] Validación completa
* [ ] Documentación/progreso actualizado

## Dependencias

* {TASK-ID, ADR-ID o ninguna}

## Notas

{notas de ejecución}
```

## Ciclo de vida

1. Crear el archivo de tarea en `04_tasks/tasks/` con estado `Borrador`.
2. Validar los campos requeridos y el nivel de riesgo antes de moverla a `Lista`.
3. Agregar el trabajo listo o activo a `04_tasks/current.md`.
4. Implementar solo una tarea `Lista` a la vez.
5. Mover el trabajo bloqueado a `04_tasks/blocked.md` con una condición de desbloqueo clara.
6. Usar `En revisión` cuando la implementación esté terminada pero falta validación o revisión humana.
7. Al completar, agregar un resumen a `04_tasks/done/YYYY-MM.md`.
8. Eliminar los archivos de tarea completados de `04_tasks/tasks/`.

## Reglas especiales para CAP

Las tareas de capacitación usan el área `CAP` y usualmente tienen rama `-`. Su salida esperada es capacidad transferida, no código fuente. Incluir agenda, materiales y resultado esperado del participante.

## Checklist

* Cada tarea activa tiene un archivo.
* `tasks/` contiene solo tareas en Borrador, Lista, En progreso, En revisión o Bloqueada.
* El trabajo completado vive en `done/YYYY-MM.md`.
* Los items del backlog no se tratan como tareas aprobadas.
* La salida esperada es lo suficientemente concreta para validar.
* Las tareas de riesgo alto muestran aprobación explícita antes de la implementación.
* El header de `current.md` tiene como máximo 1 línea `> **Última actualización:` y hasta 5 líneas `> **Completado`. No contiene otras blockquotes históricas (`> **Cerrado`, `> **Agregado`, etc.).
* `current.md` no contiene un segundo encabezado `# 04 —` ni una tabla `## Cola activa` legada.

## Errores comunes

* Implementar directamente desde el backlog.
* Implementar una tarea en `Borrador`.
* Combinar funcionalidades no relacionadas en una misma tarea.
* Mantener archivos de tareas completadas en `tasks/`.
* Usar salida esperada vaga como "mejorar módulo".
* Acumular más de 5 líneas `> **Completado` en el header de `current.md` sin limpiarlas (el MCP debe aplicar `trimCompletedHeaderLines` al cerrar cada tarea).
* Mantener estructura duplicada legacy (`# 04 — Tareas Activas` + `## Cola activa`) junto con las secciones operativas nuevas.

## Template de tarea

```markdown
# TASK-{INICIALES}-{AREA}-{NN} — {título corto}

**Estado:** Borrador | Lista | En progreso | Bloqueada | En revisión | Completada
**Autor:** {git config user.name} `<{git config user.email}>`
**Rama:** {feature/iniciales/descripcion-kebab o -}
**Inicio:** {YYYY-MM-DD HH:MM zona horaria}
**Cierre:** {YYYY-MM-DD HH:MM zona horaria o -}
**Área:** {FE | BE | HF | DB | INF | DOC | MCP | ARCH | QA | CAP}
**Prioridad:** Alta | Media | Baja
**Riesgo:** Bajo | Medio | Alto
**Aprobación:** Pendiente | Aprobada | No requerida

## Título

{título expandido}

## Contexto

{por qué existe esta tarea}

## Objetivo

{una oración describiendo el resultado}

## Alcance

### Incluye

* {trabajo incluido}

### Excluye

* {trabajo explícitamente excluido}

## Criterios de aceptación

* [ ] {comportamiento verificable}
* [ ] {salida observable}

## Archivos probables

* `{ruta o pendiente de descubrir}`

## Plan técnico

1. {paso técnico}
2. {paso técnico}

## Pasos

1. {paso}
2. {paso}

## Salida esperada

{qué debe ser verdad cuando la tarea esté completa}

## Validación

* [ ] {resultado verificable}
* [ ] {comando de validación o verificación manual}

## Rollback

{cómo revertir de forma segura si esta tarea causa problemas}

## Checklist

* [ ] Alcance aprobado
* [ ] Riesgo revisado
* [ ] Implementación completa
* [ ] Validación completa
* [ ] Documentación/progreso actualizado

## Dependencias

* {TASK-ID, ADR-ID o ninguna}

## Notas

{notas de ejecución}
```

## Ciclo de vida

1. Crear el archivo de tarea en `04_tasks/tasks/` con estado `Borrador`.
2. Validar los campos requeridos y el nivel de riesgo antes de moverla a `Lista`.
3. Agregar el trabajo listo o activo a `04_tasks/current.md`.
4. Implementar solo una tarea `Lista` a la vez.
5. Mover el trabajo bloqueado a `04_tasks/blocked.md` con una condición de desbloqueo clara.
6. Usar `En revisión` cuando la implementación esté terminada pero falta validación o revisión humana.
7. Al completar, agregar un resumen a `04_tasks/done/YYYY-MM.md`.
8. Eliminar los archivos de tarea completados de `04_tasks/tasks/`.

## Reglas especiales para CAP

Las tareas de capacitación usan el área `CAP` y usualmente tienen rama `-`. Su salida esperada es capacidad transferida, no código fuente. Incluir agenda, materiales y resultado esperado del participante.

## Checklist

* Cada tarea activa tiene un archivo.
* `tasks/` contiene solo tareas en Borrador, Lista, En progreso, En revisión o Bloqueada.
* El trabajo completado vive en `done/YYYY-MM.md`.
* Los items del backlog no se tratan como tareas aprobadas.
* La salida esperada es lo suficientemente concreta para validar.
* Las tareas de riesgo alto muestran aprobación explícita antes de la implementación.
* El header de `current.md` tiene como máximo 1 línea `> **Última actualización:` y hasta 5 líneas `> **Completado`. No contiene otras blockquotes históricas (`> **Cerrado`, `> **Agregado`, etc.).
* `current.md` no contiene un segundo encabezado `# 04 —` ni una tabla `## Cola activa` legada.

## Errores comunes

* Implementar directamente desde el backlog.
* Implementar una tarea en `Borrador`.
* Combinar funcionalidades no relacionadas en una misma tarea.
* Mantener archivos de tareas completadas en `tasks/`.
* Usar salida esperada vaga como "mejorar módulo".
* Acumular más de 5 líneas `> **Completado` en el header de `current.md` sin limpiarlas (el MCP debe aplicar `trimCompletedHeaderLines` al cerrar cada tarea).
* Mantener estructura duplicada legacy (`# 04 — Tareas Activas` + `## Cola activa`) junto con las secciones operativas nuevas.
