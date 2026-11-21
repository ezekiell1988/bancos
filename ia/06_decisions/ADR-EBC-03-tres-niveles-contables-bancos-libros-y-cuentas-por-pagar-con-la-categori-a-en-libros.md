# ADR-EBC-03 — Tres niveles contables: Bancos, Libros y Cuentas por pagar, con la categoría en Libros

**Estado:** aceptada
**Dominio:** datos
**Fecha:** 2026-11-21

## Contexto

El dueño quiere llevar las finanzas como en contabilidad. Hoy la categoría (tipo) vive en el movimiento del banco, el comprobante (nivel 2) es opcional y la factura (nivel 3) se paga con varios comprobantes (ADR-06). Eso no deja repartir un pago en varias categorías ni saber la categoría de cada producto de una factura, y el dashboard no puede bajar del movimiento a la factura.

## Decisión

Nivel 1 = Bancos: movimientos de los 3 estados de cuenta del ciclo 19 al 18 (BCR débito Karen, BAC tarjetas, BN tarjetas). Es lo real y la base; solo él suma en reportes (sigue ADR-03). Nivel 2 = Libros: el pago (colilla, SINPE, voucher o un documento que diga «pago de factura X»), nunca la factura. Va 1 a 1 con un movimiento del nivel 1. Lleva la categoría: una o varias partidas `{tipo, monto}` cuya suma es siempre igual al total del movimiento. Si un movimiento no tiene pago anotado, se crea un nivel 2 sin documento solo para ponerle la categoría. Lo que no se sabe va a «Otros» y se revisa al cerrar el ciclo. Un nivel 2 sin nivel 1 es una diferencia por conciliar, salvo que esté marcado «en tránsito» con su razón. Nivel 3 = Cuentas por pagar: la factura con proveedor, cédula jurídica, número, líneas (producto, cantidad, impuesto y total por línea), total y ajustes al final. Siempre cuelga de un nivel 2 (se crean juntos; no hay nivel 3 suelto). Un nivel 2 tiene uno o varios nivel 3, y la suma de sus nivel 3 es igual al monto del nivel 2. Cada línea del nivel 3 lleva su categoría, y las partidas del nivel 2 son la suma de esas líneas por categoría. Un sobrante que no se gastó (p. ej. vuelto devuelto por SINPE) se registra como nivel 3 de tipo «ajuste» con su respaldo y su propia categoría.

## Razón

Así el estado de cuenta sigue siendo la base, cada colón tiene categoría y se puede bajar del total del ciclo a la categoría, al movimiento y a la factura con sus productos. Las diferencias quedan a la vista como en una conciliación bancaria.

## Alternativas

* Dejar la categoría en el movimiento del nivel 1 (como hoy): no deja repartir un pago en varias categorías.
* Mantener la relación varios a varios de ADR-06 (una factura con varios pagos): complica la suma y la categoría por línea. El caso real es un pago con una o varias facturas.
* Poner la categoría solo en las líneas de la factura: deja sin categoría los movimientos que no tienen factura.

## Consecuencias

* Hay que migrar: cada movimiento con tipo pasa a un nivel 2 con una partida por el total; las facturas con varios comprobantes se pasan al nuevo modelo y se revisan a mano si no cuadran.
* El MCP bancos, el índice SQLite, el skill record-movements y el dashboard cambian para leer la categoría del nivel 2.
* Pendientes = nivel 2 sin nivel 1 (que no esté en tránsito), movimientos sin nivel 2 o con partidas que no suman, y nivel 3 cuyas sumas no cuadran con su nivel 2.
* Ejemplo 1 a 1: compra en Palí de papá con 2 artículos personales → 1 factura con líneas de 2 categorías, 1 pago con 2 partidas (Comida papi y Personal).
* Ejemplo 1 a varios: retiro de ₡5.000 para papá → factura de ₡3.000 + ajuste de ₡2.000 (pantallazo del SINPE de vuelta); el pago de ₡5.000 queda con 2 partidas (Gastos papi ₡3.000 y Personal ₡2.000).
* Reemplaza ADR-06. Complementa ADR-03, que sigue vigente en que solo el nivel 1 suma en reportes.

## Reemplaza

* ADR-06
