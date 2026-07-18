# ADR-02 — Modelo contable, clasificación y moneda

> Estado: Aceptada
> Fecha: 2026-07-18

## Decisión

Moneda funcional CRC. Movimientos guardan CRC, USD, moneda original y tipo diario único. Pasivos USD generan diferencial cambiario mensual regenerable; activos USD quedan fuera inicialmente.

Clasificación usa regla exacta por cuenta/descripción, reglas por patrón, Azure AI, categoría previa y `General`. Manual alimenta reglas y tags.

## Consecuencias

Cada cierre FX guarda encabezado y líneas detalladas. Reprocesar mes recalcula cierres futuros.
