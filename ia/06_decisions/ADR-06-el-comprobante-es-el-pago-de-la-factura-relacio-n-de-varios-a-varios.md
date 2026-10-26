# ADR-06 — El comprobante es el pago de la factura: relación de varios a varios

**Estado:** aceptada
**Dominio:** datos
**Fecha:** 2026-10-26

## Contexto

ADR-03 vinculaba la factura a un solo comprobante (campo comprobante) y a un solo movimiento. El dueño aclaró que el nivel 2 es el pago de un documento de nivel 3 y que no es 1 a 1: una factura se puede pagar con varios pagos (abonos, cuotas), y en la práctica un SINPE puede cubrir más de un recibo.

## Decisión

La factura guarda sus pagos en `comprobantes: [c-...]` (reemplaza el campo `comprobante`). Varias facturas pueden citar el mismo comprobante. Si la factura tiene comprobantes, se concilia a través de ellos: sus movimientos son los de sus comprobantes, no se empareja sola por monto y su tipo, de y nota pasan a cada uno de esos movimientos. Si no tiene comprobantes, se puede vincular directo a un movimiento (`movimiento`), como antes. Pagado = suma de sus comprobantes; saldo = total − pagado. En el índice SQLite la relación va en la tabla `factura_pagos (factura_id, comprobante_id)`. Complementa ADR-03; los reportes siguen sumando solo movimientos.

## Razón

Refleja cómo se paga de verdad (abonos, cuotas, un SINPE para varios recibos) y permite ver el saldo de cada factura sin tocar los reportes, que siguen saliendo del nivel 1.

## Alternativas

* Seguir con un solo comprobante por factura: no sirve para abonos ni cuotas.
* Guardar en el comprobante la factura que paga: obliga a anotar la factura antes que el pago.
* Monto aplicado por cada pago: más exacto cuando un pago cubre varias facturas, pero se agrega solo si hace falta.

## Consecuencias

* Si un mismo comprobante paga varias facturas, lo pagado de cada una cuenta el comprobante entero; el saldo puede verse menor de lo real.
* El vínculo automático por monto y fecha solo aplica a facturas sin comprobantes.
* No hay facturas anotadas todavía, así que no hace falta migrar datos.
