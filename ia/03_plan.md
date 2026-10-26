# 03 — Plan de Desarrollo

> Última actualización: 2026-10-25

## Dirección actual

Registro sencillo de movimientos: primero el MCP `bancos` para anotar y consultar (skill mcp-vscode), después el skill del proyecto que guía cómo procesar documentos (skill create-skill).

## Fases

### Fase 1 — Workflow /ia funcional ✅ Completada

> Avance: 3 de 3 componentes completados (actualizado 2026-10-25).
> Requisitos: ninguno (base del workflow /ia)

| Componente | Estado |
|------------|--------|
| Checklist de revisión | ✅ TASK-EBC-DOC-01 |
| Mapa de área a skill | ✅ TASK-EBC-DOC-02 |
| Contenido /ia | ✅ TASK-EBC-DOC-03 |

**Criterios de éxito:**

* `/ia` no tiene marcadores `{…}` de contenido pendiente (fuera de los placeholders operativos que el MCP, si existe, debe poblar).
* `ia/rules/` tiene las reglas genéricas del workflow y las propias del proyecto (`checklist-revision.md`, `mapa-area-skill.md`) completas.
* Nada del workflow vive fuera de `ia/`: prompts en `ia/prompts/`; instrucciones de asistentes y comandos son globales (`install-global.mjs`).

**Dependencias:**

* `TASK-EBC-DOC-03` depende de `TASK-EBC-DOC-01`, `TASK-EBC-DOC-02`.

### Fase 2 — MCP bancos ✅ Completada

> Avance: 1 de 1 componentes completados (actualizado 2026-10-25).
> Requisitos: REQ-001, REQ-002, REQ-003, REQ-004, REQ-005, REQ-006

| Componente | Estado |
|------------|--------|
| MCP bancos | ✅ TASK-EBC-MCP-01 |

**Criterios de éxito:**

* El MCP `bancos` anota, lista, resume y borra movimientos y agrega tipos, con preview y `apply: true`.
* `node .mcp/bancos/tests/smoke.mjs` termina en TODO OK con datos de prueba en una carpeta temporal.
* El MCP queda registrado en el proyecto para Claude Code, Codex y VS Code.

**Dependencias:** Fase 1.

### Fase 3 — Skill para anotar documentos 🔄 En curso

> Avance: 0 de 1 componentes completados (actualizado 2026-10-25).
> Requisitos: REQ-001, REQ-002, REQ-003, REQ-004

| Componente | Estado |
|------------|--------|
| Skill para anotar documentos | 🔄 TASK-EBC-DOC-04 |

**Criterios de éxito:**

* Existe un skill del proyecto, hecho con create-skill, que explica cómo leer un documento y anotarlo con el MCP `bancos`.
* Se prueba anotando un documento real del dueño.

**Dependencias:** Fase 2.


## Pendiente de planificación

* Desglosar las fases futuras cuando se resuelvan sus dependencias.
