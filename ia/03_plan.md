# 03 — Plan de Desarrollo

> Última actualización: 2026-10-26

## Dirección actual

Contabilidad de la familia Baltodano Soto en 3 niveles: el estado de cuenta (BCR y BAC) es la base de verdad de los reportes; los comprobantes (pagos) y facturas (documentos cobrados) son detalle auxiliar, con relación de varios a varios entre ellos (ADR-06), y SQLite para consultas. El hogar (dueño y Karen) se analiza primero y el papá es un rubro aparte, incluido por defecto. Después, el grupo de Telegram compartido con Karen.

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

### Fase 3 — Skill para anotar documentos ✅ Completada

> Avance: 1 de 1 componentes completados (actualizado 2026-10-26).
> Requisitos: REQ-001, REQ-002, REQ-003, REQ-004

| Componente | Estado |
|------------|--------|
| Skill para anotar documentos | ✅ TASK-EBC-DOC-04 |

**Criterios de éxito:**

* Existe un skill del proyecto, hecho con create-skill, que explica cómo leer un documento y anotarlo con el MCP `bancos`.
* Se prueba anotando un documento real del dueño.

**Dependencias:** Fase 2.

### Fase 4 — Grupo de Telegram para finanzas ⏳ Pendiente

> Avance: 0 de 1 componentes completados (actualizado 2026-10-25).
> Requisitos: REQ-007

| Componente | Estado |
|------------|--------|
| Grupo de prueba | ⏳ TASK-EBC-DOC-05 |

**Criterios de éxito:**

* Existe un grupo de prueba solo con el dueño y @MacEzequielBot (administrador), vinculado al proyecto Bancos.
* Un documento mandado al grupo queda anotado con el MCP `bancos` y su respaldo en `datos/multimedia/`, y el bot responde en el grupo.
* Se decide qué puede hacer la pareja en el grupo (solo consultar o también anotar) y el bot lo soporta.
* La pareja entra al grupo final y se prueba un documento suyo de punta a punta.

**Dependencias:** Fase 3 (skill record-movements probado con un documento real).

### Fase 5 — Contabilidad familiar en 3 niveles ✅ Completada

> Avance: 3 de 3 componentes completados (actualizado 2026-10-26).
> Requisitos: REQ-002, REQ-005, REQ-006, REQ-008, REQ-009, REQ-010, REQ-011, REQ-012

| Componente | Estado |
|------------|--------|
| MCP con 3 niveles y SQLite | ✅ TASK-EBC-MCP-02 |
| Migración de datos | ✅ TASK-EBC-MCP-03 |
| Skill con 3 niveles | ✅ TASK-EBC-DOC-06 |

**Criterios de éxito:**

* El MCP bancos anota comprobantes y facturas, los vincula a su movimiento y los lista; los pendientes no suman en reportes.
* bancos_resumen muestra el hogar por tipo y al papá como rubro, con excluirDe y de.
* datos/bancos.sqlite se reconstruye tras cada escritura y bancos_sql responde consultas de solo lectura.
* Los movimientos existentes tienen id y referencia, y las fotos sueltas quedaron como comprobantes.
* El skill record-movements y el README explican los 3 niveles; el smoke test termina en TODO OK.

**Dependencias:** Fase 2 (MCP bancos).

### Fase 6 — Facturas con varios pagos 🔄 En curso

> Avance: 0 de 1 componentes completados (actualizado 2026-10-26).
> Requisitos: REQ-013

| Componente | Estado |
|------------|--------|
| Pagos de facturas en el MCP | 🔄 TASK-EBC-MCP-04 |

**Criterios de éxito:**

* bancos_factura y bancos_vincular aceptan varios comprobantes por factura y un comprobante en varias facturas.
* bancos_detalles muestra lo pagado y el saldo de cada factura; bancos_pendientes lista las facturas con saldo.
* SQLite tiene la tabla factura_pagos y bancos_sql la describe.
* Skill record-movements, README y smoke test al día (TODO OK).

**Dependencias:** Fase 5 (3 niveles).


## Pendiente de planificación

* Desglosar las fases futuras cuando se resuelvan sus dependencias.
