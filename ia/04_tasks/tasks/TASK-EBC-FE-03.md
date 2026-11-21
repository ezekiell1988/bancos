# TASK-EBC-FE-03 — Dashboard en 3 niveles: ciclo → categoría → movimiento → factura

**schemaVersion:** 2
**Estado:** Borrador
**DevOps:** —
**CRM:** —
**Autor:** Ezequiel Baltodano Cubillo `<ezekiell1988@hotmail.com>`
**Aprobado por:** —
**Rama:** —
**createdAt:** 2026-11-21T10:29:42.388-06:00
**approvedAt:** —
**startedAt:** —
**finishedAt:** —
**Área:** FE
**Prioridad:** alta
**Riesgo:** medio
**Aprobación:** pendiente
**parentId:** —
**workItemType:** Task
**workstream:** ia-workflow
**tags:** fe, ia-workflow
**Requisitos:** —

---

## Título

Dashboard en 3 niveles: ciclo → categoría → movimiento → factura

## Contexto

Con ADR-EBC-03 la categoría vive en Libros (nivel 2) y la factura (nivel 3) tiene la categoría por línea. El dashboard debe bajar del total del ciclo a la factura. La pestaña de conciliación ya está separada y se deja igual.

## Objetivo

Que el dashboard muestre el ciclo 19→18 como en contabilidad: ingresos vs gastos, luego por categoría, luego movimientos y al final la factura con sus productos y su imagen.

## Alcance permitido

* dashboard/

## Fuera de alcance

* La pestaña de conciliación (se deja como está, salvo leer los pendientes nuevos)
* Cambios del modelo de datos (TASK-EBC-MCP-10)

## Criterios de aceptación

* Arriba: ingresos vs gastos del ciclo (nivel 1).
* Debajo: ingresos y gastos por categoría (partidas del nivel 2). Un movimiento repartido en 2 categorías aparece en las dos con su monto.
* Al tocar una categoría: lista de movimientos del nivel 1 de esa categoría (fecha, cuenta, comercio, monto de la partida y total).
* Al tocar un movimiento con nivel 3: detalle agrupado por factura con proveedor, cédula jurídica, productos con impuesto, total y categoría de cada línea, ajustes y total; botón para ver la imagen.
* Si el movimiento no tiene nivel 3, se ve solo el movimiento y sus partidas, sin detalle de factura.
* «Atrás» en cada nivel y «Enviar al chat» siguen funcionando.
* La pestaña de conciliación queda separada como hoy.
* Sin scroll horizontal a 375 px y en escritorio, con capturas antes y después.

## Archivos afectados / probables

* dashboard/server.mjs
* dashboard/render.mjs

## Plan técnico

* Leer de las vistas SQLite nuevas de MCP-10 (ciclo → categoría → movimiento → factura).
* Rutas en dashboard/server.mjs para cada nivel y para servir la imagen del respaldo desde datos/multimedia.
* Vistas en render.mjs reutilizando la navegación con Atrás y el envío de imagen al chat.
* Capturas antes/después a 375 y 1440 px.

## Pasos

1. Captura «antes»
2. Datos por nivel en server.mjs
3. Pantallas en render.mjs
4. Ver imagen de la factura
5. Medir 375 px y escritorio
6. Captura «después»

## Salida esperada

Dashboard que baja de ingresos vs gastos a categorías, movimientos y facturas con sus líneas e imagen.

## Validación

* node --check dashboard/server.mjs dashboard/render.mjs
* Recorrido completo en el ciclo 19-oct al 18-nov con los 2 ejemplos del ADR
* scrollWidth sin desborde a 375 px y 1440 px

## Reversión

Revertir el commit de dashboard/ y reiniciar el servicio launchd com.ezekl.bancos-dashboard.

## Dependencias

* TASK-EBC-MCP-10 (modelo de 3 niveles y vistas SQLite)

## Notas

—

## Issues vinculados

* Ninguno.

## Historial de eventos V2

```jsonl
{"schemaVersion":2,"eventId":"658d97e2-7ff2-4be7-9325-e196f574902f","taskId":"TASK-EBC-FE-03","event":"created","occurredAt":"2026-11-21T10:29:42.388-06:00","actor":"MCP iaWorkflow","reason":"Tarea creada en Borrador."}
```
