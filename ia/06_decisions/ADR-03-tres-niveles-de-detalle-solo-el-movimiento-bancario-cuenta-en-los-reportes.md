# ADR-03 — Tres niveles de detalle: solo el movimiento bancario cuenta en los reportes

**Estado:** aceptada
**Dominio:** datos
**Fecha:** 2026-10-26

## Contexto

El dueño pasa estados de cuenta (HTML del BCR, PDF de la BAC), comprobantes del momento (SINPE, vouchers) y facturas con detalle (supermercado, servicios públicos). Hasta ahora todo se anotaba como movimiento y las fotos sueltas entraban con fecha provisional, lo que mezclaba la base de verdad con documentos auxiliares.

## Decisión

Nivel 1 = movimiento bancario (datos/<cuenta>/AAAA-MM/AAAA-MM-DD.jsonl), con id estable y referencia del banco: es la base de verdad y lo único que suma en reportes. Nivel 2 = comprobante (datos/comprobantes/AAAA-MM/AAAA-MM-DD.jsonl) con su referencia y el id del movimiento. Nivel 3 = factura (datos/facturas/AAAA-MM/AAAA-MM-DD.jsonl) con sus líneas y el id del movimiento (y del comprobante, si hay). Un comprobante o factura sin movimiento queda pendiente; al anotar el estado se vincula solo (primero por referencia, después por monto, moneda y fecha cercana) y su tipo, de y nota completan el movimiento. Esto reemplaza la parte de ADR-02 que anotaba fotos y SINPE sueltos como movimientos con fecha provisional; la regla de la fecha del banco sigue igual.

## Razón

El estado de cuenta es lo único completo y oficial; los comprobantes y facturas solo aclaran qué fue cada línea. Separarlos evita duplicados y que un reporte cambie según qué fotos llegaron.

## Alternativas

* Seguir anotando comprobantes como movimientos provisionales y reubicarlos al llegar el estado: mezcla niveles y depende de que el comercio se escriba igual.
* Guardar el detalle dentro del movimiento: obliga a tener el estado antes que el comprobante.

## Consecuencias

* Lo gastado después del último estado no aparece en los reportes hasta que llegue el siguiente; se ve como pendiente.
* Los movimientos existentes necesitan id y referencia (migración única).
* Las fotos que ya estaban anotadas como movimientos del BCR pasan a comprobantes pendientes.
