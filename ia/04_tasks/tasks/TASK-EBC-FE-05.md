# TASK-EBC-FE-05 — Panel: deuda de tarjeta con el pago de contado y botón «Ver en colones»

**schemaVersion:** 2
**Estado:** Lista
**DevOps:** —
**CRM:** —
**Autor:** Ezequiel Baltodano Cubillo `<ezekiell1988@hotmail.com>`
**Aprobado por:** EBC (pedido por Telegram 21-nov-2026)
**Rama:** —
**createdAt:** 2026-11-21T12:38:03.680-06:00
**approvedAt:** 2026-11-21T12:38:07.364-06:00
**startedAt:** —
**finishedAt:** —
**Área:** FE
**Prioridad:** media
**Riesgo:** bajo
**Aprobación:** aprobada
**parentId:** —
**workItemType:** Task
**workstream:** ia-workflow
**tags:** fe, ia-workflow
**Requisitos:** —

---

## Título

Panel: deuda de tarjeta con el pago de contado y botón «Ver en colones»

## Contexto

Pedido del dueño (Telegram 21-nov-2026): la sección «Deuda de tarjeta» debajo de «Traspasos a tarjeta» no concuerda con el «Pago de contado». Depende de TASK-EBC-MCP-13, que cambia deudaTarjeta de bancos_resumen para usar el contado del estado de cuenta y dar la conversión a colones.

## Objetivo

Mostrar en el panel la deuda de tarjeta con los datos nuevos de MCP-13 y un botón para verla en colones.

## Alcance permitido

* dashboard/render.mjs
* dashboard/server.mjs (solo si hace falta el parámetro de la vista)

## Fuera de alcance

* Lógica de cálculo de la deuda (MCP-13)
* Otras secciones del panel

## Criterios de aceptación

* «Deuda de tarjeta» muestra por tarjeta el corte del estado, el pago de contado (₡ y $), lo pagado y la diferencia, con los mismos montos que «Pago de contado de las tarjetas» para el mismo estado.
* Botón «Ver en colones» (y volver a «por moneda») que pasa los dólares a colones y muestra el tipo de cambio usado y su fuente: «del pago» si ya se pagó, «BAC del día» si no. Funciona en la Mini App de Telegram sin recargar el periodo.
* Ciclo 2026-11: BAC al día; en colones ₡1.007.000 contra ₡1.007.000 con tipo de cambio del pago. Ciclo 2026-12: en colones igual a «Falta por pagar» del pago de contado.
* Sin desborde a 375 px ni en escritorio (scrollWidth de la página y la tabla), revisado por el túnel.
* Reiniciar el panel al cerrar (lo pide la experiencia de FE-04).

## Archivos afectados / probables

* dashboard/render.mjs

## Plan técnico

* Reescribir deuda(d, r) en render.mjs con los campos nuevos de deudaTarjeta.
* Dos vistas en el mismo HTML (por moneda / en colones) con un botón que alterna una clase; sin dependencias nuevas.
* En la imagen (img=1) mostrar la vista en colones.

## Pasos

1. Captura «antes» con el TASK-ID.
2. Cambiar deuda() y estilos.
3. Reiniciar el panel con el agente entorno.
4. Captura «después» y medición a 375 y 1440.

## Salida esperada

Deuda de tarjeta cuadrada con el pago de contado y botón para verla en colones.

## Validación

* Capturas antes/después a 375 y 1440 por el túnel
* Montos del ciclo 2026-11 y 2026-12 comparados con bancos_pago_tarjetas

## Reversión

git revert del commit de la tarea.

## Dependencias

* TASK-EBC-MCP-13

## Notas

—

## Issues vinculados

* Ninguno.

## Historial de eventos V2

```jsonl
{"schemaVersion":2,"eventId":"c789a904-1316-4d48-980f-3b64a381a9df","taskId":"TASK-EBC-FE-05","event":"created","occurredAt":"2026-11-21T12:38:03.680-06:00","actor":"MCP iaWorkflow","reason":"Tarea creada en Borrador."}
{"schemaVersion":2,"eventId":"fcb3bf35-7f10-4031-9287-4d9bc7827d85","taskId":"TASK-EBC-FE-05","event":"approved","occurredAt":"2026-11-21T12:38:07.364-06:00","actor":"MCP iaWorkflow","reason":"EBC (pedido por Telegram 21-nov-2026)"}
```
