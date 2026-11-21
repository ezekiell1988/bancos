# TASK-EBC-FE-06 — Deuda de tarjeta: botón para recalcular con el dólar BAC de hoy solo si falta pagar

**schemaVersion:** 2
**Estado:** Lista
**DevOps:** —
**CRM:** —
**Autor:** Ezequiel Baltodano Cubillo `<ezekiell1988@hotmail.com>`
**Aprobado por:** aprobación explícita
**Rama:** —
**createdAt:** 2026-11-21T13:22:59.267-06:00
**approvedAt:** 2026-11-21T13:23:01.362-06:00
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

Deuda de tarjeta: botón para recalcular con el dólar BAC de hoy solo si falta pagar

## Contexto

Revisión del dueño de FE-05 (ciclo 2026-12 en el celular): en una tarjeta que no se ha pagado no ve un botón para recalcular con el tipo de cambio actual del BAC; en el ciclo ya pagado (2026-11) ese botón no debe salir, porque el cambio queda fijo por el pago. Hoy el botón «Ver en colones / Ver por moneda» es uno solo para todo el bloque y sale siempre. bancos_resumen ya trae el cambio del día en vivo de InfoDolar (fuente «bac del dia» / «bn del dia») cuando no hay pago que lo fije.

## Objetivo

En «Deuda de tarjeta», el botón de recalcular con el dólar del día sale solo en las tarjetas con saldo pendiente (cambio que no viene del pago); en las pagadas no sale y se muestra el monto en colones con el cambio del pago.

## Alcance permitido

* dashboard/render.mjs
* dashboard/server.mjs (solo si hace falta una ruta GET para volver a pedir el cambio)
* .mcp/bancos/tools/bancos_resumen.mjs (solo si hace falta forzar el cambio del día sin caché)

## Fuera de alcance

* Cambiar el cálculo de la deuda o el criterio del cambio del pago (MCP-13)
* La fila Total pegada de «Pago de contado de las tarjetas»

## Criterios de aceptación

* Tarjeta con saldo pendiente (cambio «BAC del día» o «BN del día»): sale un botón «Recalcular con el dólar BAC de hoy» (o BN según la tarjeta) en su fila, que vuelve a pedir el tipo de cambio actual y actualiza pago de contado en colones y diferencia, mostrando el valor y la hora del cambio usado.
* Tarjeta pagada (cambio «del pago») o al día: no sale el botón; se ve el monto en colones con el cambio del pago.
* Ciclo 2026-12: BAC pendiente con botón visible. Ciclo 2026-11: BAC sin botón.
* Funciona en la Mini App de Telegram (solo GET, rutas relativas) y en la imagen de «Enviar al chat» no aparece el botón.
* Sin desborde a 375 px ni en 1440 px, revisado por el túnel con capturas; panel reiniciado al cerrar.

## Archivos afectados / probables

* No definidos.

## Plan técnico

* En deuda(d, r) decidir por fila: si tipoCambio.fuente === 'pago' o diferencia al día, sin botón; si no, botón por fila.
* El botón hace un GET relativo (p. ej. a una ruta del server que devuelve el resumen de deuda del ciclo con el cambio recién pedido a InfoDolar) y reemplaza los montos de esa fila sin recargar el periodo.
* Revisar si cambioInfodolar cachea; si cachea, permitir forzar la consulta nueva solo para esta ruta.
* Decidir si se mantiene el conmutador por moneda/en colones para filas pendientes y documentarlo en la tarea.

## Pasos

1. work_task y cargar el skill de FE.
2. Implementar en render.mjs (y server.mjs si hace falta la ruta).
3. Reiniciar el panel (launchctl kickstart -k gui/$UID/com.ezekl.bancos-dashboard).
4. Pedir al dueño un enlace nuevo con bancos_dashboard_enlace (le llega por WhatsApp) y capturar 2026-11 y 2026-12 a 375 y 1440 con el subagente capturas.
5. finish_task, commit y push.

## Salida esperada

Bloque «Deuda de tarjeta» con botón de recalcular solo en filas pendientes, verificado con capturas por el túnel.

## Validación

* node --check dashboard/render.mjs dashboard/server.mjs
* Capturas por el túnel de 2026-11 (sin botón en BAC) y 2026-12 (con botón, y tras pulsarlo) a 375 y 1440, sin scroll lateral.

## Reversión

git revert del commit de la tarea y reiniciar el panel.

## Dependencias

* TASK-EBC-FE-05 (cerrada)

## Notas

—

## Issues vinculados

* Ninguno.

## Historial de eventos V2

```jsonl
{"schemaVersion":2,"eventId":"ede8f8cb-e2d8-49f6-8250-c8cf48b88e1e","taskId":"TASK-EBC-FE-06","event":"created","occurredAt":"2026-11-21T13:22:59.267-06:00","actor":"MCP iaWorkflow","reason":"Tarea creada en Borrador."}
{"schemaVersion":2,"eventId":"0333cbeb-5676-4f41-afea-78350f8ce3a1","taskId":"TASK-EBC-FE-06","event":"approved","occurredAt":"2026-11-21T13:23:01.363-06:00","actor":"MCP iaWorkflow","reason":"aprobación explícita"}
```
