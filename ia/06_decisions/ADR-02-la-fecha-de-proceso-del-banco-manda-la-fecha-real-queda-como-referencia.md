# ADR-02 — La fecha de proceso del banco manda; la fecha real queda como referencia

**Estado:** aceptada
**Dominio:** datos
**Fecha:** 2026-10-26

## Contexto

El banco corta los estados por fecha de proceso, no de compra. Con la fecha de compra, los totales de un ciclo no cuadraban con el estado de cuenta (compras de los últimos 2-3 días antes del corte).

## Decisión

Cada movimiento usa `fecha` = fecha de proceso del banco (BCR «Fecha contable»; BAC, MMDD inicial de la referencia; intereses y cuotas de tasa cero, la fecha de corte). Es la que usan los archivos del día, los reportes y los ciclos. `fechaReal` (opcional) guarda la fecha de compra solo como referencia, si difiere. Fotos y comprobantes sueltos se anotan con fecha provisional y `bancos_registrar` los reubica cuando llega el estado.

## Razón

El dueño pidió que lo que vale para todo el sistema sea la fecha del banco, para que los ciclos cuadren exacto con los estados.

## Alternativas

* Fecha real de compra como fecha principal (no cuadra con el banco)
* Guardar ambas y elegir en cada reporte (más complejo)

## Consecuencias

* Los 419 movimientos ya anotados se pasaron a la fecha del banco (261 con fechaReal); respaldo previo en datos/respaldos/
* Los duplicados se detectan por cualquiera de las dos fechas
* Una compra de fin de semana cae en el día hábil en que el banco la procesa
