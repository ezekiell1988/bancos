# TASK-EBC-MCP-12 — Pago de tarjeta como traspaso y control de deuda de tarjeta

**schemaVersion:** 2
**Estado:** Lista
**DevOps:** —
**CRM:** —
**Autor:** Ezequiel Baltodano Cubillo `<ezekiell1988@hotmail.com>`
**Aprobado por:** aprobación explícita
**Rama:** —
**createdAt:** 2026-11-21T11:56:22.519-06:00
**approvedAt:** 2026-11-21T11:58:48.723-06:00
**startedAt:** —
**finishedAt:** —
**Área:** MCP
**Prioridad:** alta
**Riesgo:** medio
**Aprobación:** aprobada
**parentId:** —
**workItemType:** Task
**workstream:** ia-workflow
**tags:** mcp, ia-workflow
**Requisitos:** —

---

## Título

Pago de tarjeta como traspaso y control de deuda de tarjeta

## Contexto

Hoy el resumen cuenta dos veces lo de la tarjeta: las compras de la BAC (y BN) se anotan una por una, y además el pago de la tarjeta desde el BCR (tipo «tarjeta», p. ej. ₡1.150.000 en el ciclo 19-oct → 18-nov) suma como gasto. Solo se evita pidiendo excluirTipos: ["tarjeta"]. El dueño aprobó los 3 puntos: el pago pasa a ser traspaso, se controla contra el total de la tarjeta del ciclo anterior, y el resumen lo saca siempre.

## Objetivo

Que el pago de la tarjeta no sume como gasto en ningún reporte, que se vea aparte como traspaso, y que se compare con lo que debía pagarse del ciclo anterior para mostrar la deuda de tarjeta pendiente.

## Alcance permitido

* .mcp/bancos/tools/bancos_resumen.mjs
* .mcp/bancos/src/datos.mjs
* .mcp/bancos/tools/bancos_tipos.mjs
* .mcp/bancos/tests/smoke.mjs
* .mcp/bancos/README.md
* datos/tipos-gasto.jsonl
* .agents/skills/record-movements/SKILL.md
* .codex/skills/record-movements/SKILL.md

## Fuera de alcance

* Cambios visuales del panel (dashboard/): van en su tarea FE
* Borrar o reclasificar movimientos ya anotados a mano: el cambio es de cálculo, no de datos
* Cambiar bancos_pago_tarjetas o bancos_pago_tarjeta_bn salvo reusar su lógica

## Criterios de aceptación

* El tipo «tarjeta» queda marcado como traspaso (p. ej. traspaso: true en tipos-gasto.jsonl) y bancos_tipos lo muestra
* bancos_resumen, sin argumentos extra, no suma los traspasos en balance, rubros ni tipos de gasto
* bancos_resumen devuelve un bloque aparte «traspasos» con el total pagado a cada tarjeta en el periodo
* bancos_resumen devuelve un bloque «deudaTarjeta» por tarjeta y moneda: total de compras del ciclo anterior, pagado en este periodo y diferencia (pendiente si se pagó menos, a favor si se pagó más)
* Si el ciclo anterior de una tarjeta no tiene compras anotadas, el control lo marca como «sin estado de cuenta» en vez de dar una deuda falsa
* excluirTipos sigue funcionando para otros tipos; incluirTraspasos: true permite volver a ver el pago como gasto si se pide
* El panel (que usa bancos_resumen) deja de contar el pago en Gastos sin tocar dashboard/
* Smoke del MCP en verde con un caso de traspaso y uno de deuda

## Archivos afectados / probables

* .mcp/bancos/tools/bancos_resumen.mjs
* .mcp/bancos/src/datos.mjs
* datos/tipos-gasto.jsonl

## Plan técnico

* Marcar en tipos-gasto.jsonl el tipo tarjeta con traspaso: true y exponer en datos.mjs un helper esTraspaso(tipo)
* En bancos_resumen separar las partidas de traspaso antes de sumar; armar el bloque traspasos (por tarjeta destino y moneda)
* Calcular el ciclo anterior con periodo.mjs (19→18) y sumar las compras por tarjeta y moneda; compararlas con los traspasos del periodo para deudaTarjeta
* Agregar incluirTraspasos al inputSchema y ajustar la descripción de la tool y el README
* Actualizar la regla del skill record-movements (línea del pago BAC desde el BCR) para que diga que es traspaso y ya no hace falta excluirTipos

## Pasos

1. work_task
2. Marcar el tipo y crear el helper
3. Cambiar bancos_resumen (traspasos, deudaTarjeta, incluirTraspasos)
4. Smoke con casos nuevos
5. Probar con el ciclo 2026-10-19 → 2026-11-18 y comparar contra el gasto real de antes
6. Actualizar README y skill
7. Revisión con rules/revision-codigo.md y finish_task

## Salida esperada

bancos_resumen da el gasto real por defecto, con los pagos de tarjeta aparte como traspasos y un control de deuda de tarjeta por ciclo.

## Validación

* cd .mcp && npm run smoke (o el smoke de bancos)
* bancos_resumen del ciclo 2026-10-19 → 2026-11-18: gastos ya no incluyen los ₡1.150.000 y aparecen en traspasos
* deudaTarjeta de BAC cuadra con el total del estado de cuenta del ciclo anterior menos lo pagado

## Reversión

Revertir el commit: el cálculo vuelve a sumar el pago como gasto; los datos anotados no cambian.

## Dependencias

* Ninguna.

## Notas

—

## Issues vinculados

* Ninguno.

## Historial de eventos V2

```jsonl
{"schemaVersion":2,"eventId":"eff7a3dd-f02b-4cc2-8160-9068a8ecdebc","taskId":"TASK-EBC-MCP-12","event":"created","occurredAt":"2026-11-21T11:56:22.519-06:00","actor":"MCP iaWorkflow","reason":"Tarea creada en Borrador."}
{"schemaVersion":2,"eventId":"b90e689e-297e-413e-b500-bbbf64257dab","taskId":"TASK-EBC-MCP-12","event":"approved","occurredAt":"2026-11-21T11:58:48.723-06:00","actor":"MCP iaWorkflow","reason":"aprobación explícita"}
```
