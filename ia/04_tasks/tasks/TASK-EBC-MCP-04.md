# TASK-EBC-MCP-04 — Facturas pagadas con varios comprobantes

**schemaVersion:** 2
**Estado:** En progreso
**DevOps:** —
**CRM:** —
**Autor:** Ezequiel Baltodano Cubillo `<ezekiell1988@hotmail.com>`
**Aprobado por:** aprobación explícita
**Rama:** —
**createdAt:** 2026-10-26T08:06:14.566-06:00
**approvedAt:** 2026-10-26T08:06:17.403-06:00
**startedAt:** 2026-10-26T08:06:19.143-06:00
**finishedAt:** —
**Área:** MCP
**Prioridad:** media
**Riesgo:** bajo
**Aprobación:** aprobada
**parentId:** —
**workItemType:** Task
**workstream:** ia-workflow
**tags:** mcp, ia-workflow
**Requisitos:** REQ-013

---

## Título

Facturas pagadas con varios comprobantes

## Contexto

El comprobante (nivel 2) es el pago de la factura (nivel 3) y la relación no es 1 a 1 (ADR-06). Hoy la factura admite un solo `comprobante`.

## Objetivo

Que una factura guarde varios comprobantes de pago, muestre pagado y saldo, y se concilie a través de ellos.

## Alcance permitido

* .mcp/bancos/
* .agents/skills/record-movements/SKILL.md
* README.md
* ia/

## Fuera de alcance

* Monto aplicado por pago
* Cambios en los reportes de nivel 1
* datos/

## Criterios de aceptación

* bancos_factura acepta `comprobantes: [c-...]` y no se empareja por monto si los tiene
* Al vincular un comprobante a su movimiento, las facturas que lo citan le completan tipo, de y nota
* bancos_vincular agrega o quita comprobantes de una factura
* bancos_detalles muestra pagado y saldo; bancos_detalles { movimiento } incluye facturas a través de sus comprobantes
* bancos_pendientes lista facturas con saldo
* SQLite con tabla factura_pagos; smoke TODO OK

## Archivos afectados / probables

* No definidos.

## Plan técnico

* vinculos.mjs: planVinculos usa f.comprobantes; helper de pagado/saldo
* bancos_factura: campo comprobantes (array); acepta el viejo comprobante
* bancos_vincular: parámetro comprobantes / quitar
* bancos_detalles, bancos_pendientes, bancos_movimientos: facturas vía comprobantes y saldo
* sqlite.mjs: tabla factura_pagos y vista facturas_saldo; bancos_sql describe
* Smoke tests, README, SKILL

## Pasos

1. Cambiar vinculos.mjs y bancos_factura para comprobantes en lista
2. Ajustar vincular, detalles, pendientes, movimientos y SQLite
3. Actualizar smoke, README y skill; correr smoke

## Salida esperada

MCP bancos con facturas de varios pagos, saldo visible y smoke OK.

## Validación

* node .mcp/bancos/tests/smoke.mjs

## Reversión

git checkout de .mcp/bancos; no hay facturas en datos/.

## Dependencias

* Ninguna.

## Notas

—

## Issues vinculados

* Ninguno.

## Historial de eventos V2

```jsonl
{"schemaVersion":2,"eventId":"41ca09b6-bf88-4f7f-acf2-11b6775d3fc7","taskId":"TASK-EBC-MCP-04","event":"created","occurredAt":"2026-10-26T08:06:14.566-06:00","actor":"MCP iaWorkflow","reason":"Tarea creada en Borrador."}
{"schemaVersion":2,"eventId":"73b9c4e3-863b-4d98-b6ca-b61db174dcf1","taskId":"TASK-EBC-MCP-04","event":"approved","occurredAt":"2026-10-26T08:06:17.404-06:00","actor":"MCP iaWorkflow","reason":"aprobación explícita"}
{"schemaVersion":2,"eventId":"127c5eec-96a1-49b6-a06a-9dcfb53c176e","taskId":"TASK-EBC-MCP-04","event":"started","occurredAt":"2026-10-26T08:06:19.144-06:00","actor":"MCP iaWorkflow","reason":"Inicio de implementación autorizado."}
```
