# TASK-EBC-MCP-10 — Modelo de 3 niveles contables en el MCP bancos (categoría en Libros)

**schemaVersion:** 2
**Estado:** Lista
**DevOps:** —
**CRM:** —
**Autor:** Ezequiel Baltodano Cubillo `<ezekiell1988@hotmail.com>`
**Aprobado por:** Ezequiel Baltodano Cubillo (EBC), por Telegram: opción A
**Rama:** —
**createdAt:** 2026-11-21T10:29:28.566-06:00
**approvedAt:** 2026-11-21T10:32:24.966-06:00
**startedAt:** —
**finishedAt:** —
**Área:** MCP
**Prioridad:** alta
**Riesgo:** alto
**Aprobación:** aprobada
**parentId:** —
**workItemType:** Task
**workstream:** ia-workflow
**tags:** mcp, ia-workflow
**Requisitos:** —

---

## Título

Modelo de 3 niveles contables en el MCP bancos (categoría en Libros)

## Contexto

ADR-EBC-03 define Bancos (1), Libros (2) y Cuentas por pagar (3), con la categoría en el nivel 2 repartida en partidas por monto y la relación 2→3 por líneas. Hoy la categoría vive en el movimiento y la factura se paga con varios comprobantes (ADR-06, reemplazada).

## Objetivo

Que el MCP bancos, el índice SQLite y el skill trabajen con el modelo de ADR-EBC-03 y que los datos actuales queden migrados sin perder nada.

## Alcance permitido

* .mcp/bancos/
* .agents/skills/record-movements/
* README.md
* ia/02_architecture.md
* datos/ (migración con respaldo previo)

## Fuera de alcance

* Cambios visuales del dashboard (los hace la tarea FE)
* Inventar categorías de movimientos que no se conocen: van a «Otros»

## Criterios de aceptación

* Nivel 2 tiene `movimiento` (1 a 1 con un nivel 1), `partidas: [{tipo, monto, de?, nota?}]` que suman el total del movimiento, `documento?` (respaldo opcional) y `transito?: {razon}`.
* Todo movimiento del nivel 1 tiene un nivel 2; si no hay pago anotado se crea sin documento para la categoría (default «Otros»).
* Nivel 3 lleva proveedor, cédula jurídica, número, líneas con producto, cantidad, impuesto, total y `tipo` por línea, ajustes al final y `clase` factura o ajuste; siempre cuelga de un nivel 2 (`libro`). No se puede crear un nivel 3 sin nivel 2.
* Validación: suma de nivel 3 de un nivel 2 = monto del nivel 2, y partidas del nivel 2 = suma de líneas por categoría cuando hay nivel 3.
* `bancos_pendientes` lista: nivel 2 sin nivel 1 (menos los en tránsito), movimientos sin categoría o en «Otros», y sumas que no cuadran.
* `bancos_resumen` saca ingresos y gastos por categoría desde las partidas del nivel 2 sobre los montos del nivel 1.
* Migración con respaldo: cada movimiento con tipo pasa a un nivel 2 con una partida; comprobantes y facturas existentes se reubican; un informe lista lo que no cuadró.
* SQLite con tablas libros, libro_partidas, facturas, factura_lineas (con tipo) y vistas para el dashboard (ciclo → categoría → movimiento → factura).
* Smoke con los 2 ejemplos del ADR (Palí 1 a 1 con 2 categorías; retiro de ₡5.000 con factura de ₡3.000 y ajuste de ₡2.000).
* Skill record-movements y 02_architecture.md actualizados.

## Archivos afectados / probables

* .mcp/bancos/tools/*.mjs
* .mcp/bancos/tests/smoke.mjs
* .agents/skills/record-movements/SKILL.md
* ia/02_architecture.md
* README.md

## Plan técnico

* Respaldar `datos/` antes de tocar nada.
* Definir los nuevos contratos de nivel 2 y 3 y sus validaciones en .mcp/bancos.
* Adaptar bancos_comprobante (nivel 2 con partidas), bancos_factura (nivel 3 con líneas y tipo por línea, ajustes), bancos_vincular, bancos_editar_movimiento (la categoría se edita en el nivel 2) y bancos_registrar (crea el nivel 2 al anotar el estado).
* Script de migración idempotente con modo vista previa e informe.
* Reconstruir SQLite con las tablas y vistas nuevas.
* Actualizar pendientes, resumen, smoke, skill y arquitectura.

## Pasos

1. Respaldo de datos/
2. Contratos y validaciones
3. Tools del MCP
4. Migración en vista previa → revisar informe → aplicar
5. SQLite y vistas
6. Smoke con los 2 ejemplos
7. Skill y docs

## Salida esperada

MCP bancos con el modelo de 3 niveles de ADR-EBC-03, datos migrados con informe de lo que no cuadró, smoke en verde y skill al día.

## Validación

* node --check .mcp/bancos/server.mjs
* node .mcp/bancos/tests/smoke.mjs
* Informe de migración: total del ciclo antes = después
* bancos_pendientes del ciclo 19-oct al 18-nov

## Reversión

Restaurar el respaldo de datos/ y revertir el commit; reconstruir SQLite.

## Dependencias

* Ninguna.

## Notas

Relación con TASK-EBC-MCP-04 (facturas con varios comprobantes, En progreso): ADR-06 queda reemplazada, así que esa tarea se cierra o se absorbe aquí.

## Issues vinculados

* Ninguno.

## Historial de eventos V2

```jsonl
{"schemaVersion":2,"eventId":"53404f3a-fb44-481b-bb10-76ba828e80aa","taskId":"TASK-EBC-MCP-10","event":"created","occurredAt":"2026-11-21T10:29:28.566-06:00","actor":"MCP iaWorkflow","reason":"Tarea creada en Borrador."}
{"schemaVersion":2,"eventId":"200ac533-738c-4ef0-b52d-9d8df93f310a","taskId":"TASK-EBC-MCP-10","event":"approved","occurredAt":"2026-11-21T10:32:24.967-06:00","actor":"MCP iaWorkflow","reason":"Ezequiel Baltodano Cubillo (EBC), por Telegram: opción A"}
```
