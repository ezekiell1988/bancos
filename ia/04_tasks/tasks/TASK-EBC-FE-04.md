# TASK-EBC-FE-04 — Panel: traspasos y deuda de tarjeta aparte de los gastos

**schemaVersion:** 2
**Estado:** Borrador
**DevOps:** —
**CRM:** —
**Autor:** Ezequiel Baltodano Cubillo `<ezekiell1988@hotmail.com>`
**Aprobado por:** —
**Rama:** —
**createdAt:** 2026-11-21T11:56:33.217-06:00
**approvedAt:** —
**startedAt:** —
**finishedAt:** —
**Área:** FE
**Prioridad:** media
**Riesgo:** bajo
**Aprobación:** pendiente
**parentId:** —
**workItemType:** Task
**workstream:** ia-workflow
**tags:** fe, ia-workflow
**Requisitos:** —

---

## Título

Panel: traspasos y deuda de tarjeta aparte de los gastos

## Contexto

TASK-EBC-MCP-12 hace que bancos_resumen deje de sumar el pago de la tarjeta como gasto y devuelva los bloques traspasos y deudaTarjeta. El panel ya deja de contarlo en Gastos solo, pero falta mostrar esos dos bloques para que el dueño vea cuánto pagó a cada tarjeta y si queda deuda.

## Objetivo

Mostrar en el resumen del panel los traspasos a tarjetas y el control de deuda de tarjeta, separados de ingresos y gastos.

## Alcance permitido

* dashboard/render.mjs
* dashboard/server.mjs

## Fuera de alcance

* Cálculos del MCP (van en TASK-EBC-MCP-12)
* Rediseño de otras pestañas

## Criterios de aceptación

* El resumen muestra un chip o fila «Traspasos a tarjeta» por moneda que no suma en Gastos ni en el balance
* Hay una tarjeta «Deuda de tarjeta» con compras del ciclo anterior, pagado y diferencia por tarjeta; la diferencia pendiente se destaca y «sin estado de cuenta» se muestra como aviso
* Tocar un traspaso lleva al movimiento del pago (nivel 3 existente)
* Sin scroll horizontal a 375 y 1440 px, también con el menú móvil abierto

## Archivos afectados / probables

* dashboard/render.mjs

## Plan técnico

* Leer traspasos y deudaTarjeta del resultado de bancos_resumen en render.mjs
* Agregar la fila/chip de traspasos y una tarjeta de deuda siguiendo el estilo de las tarjetas existentes
* Medir con Playwright ancho y solapes a 375 y 1440

## Pasos

1. work_task
2. Capturas «antes»
3. Agregar bloques en render.mjs
4. Capturas «después» y medición
5. Revisión y finish_task

## Salida esperada

El panel muestra los pagos de tarjeta como traspasos y la deuda de tarjeta del ciclo, aparte de los gastos.

## Validación

* Capturas antes/después a 375 y 1440 px del ciclo 2026-10-19 → 2026-11-18
* scrollWidth de página y tablas ≤ ancho de ventana

## Reversión

Revertir el commit del panel; el MCP sigue funcionando igual.

## Dependencias

* TASK-EBC-MCP-12

## Notas

—

## Issues vinculados

* Ninguno.

## Historial de eventos V2

```jsonl
{"schemaVersion":2,"eventId":"1f74201c-bb38-4cf2-a411-ba492d5cd197","taskId":"TASK-EBC-FE-04","event":"created","occurredAt":"2026-11-21T11:56:33.217-06:00","actor":"MCP iaWorkflow","reason":"Tarea creada en Borrador."}
```
