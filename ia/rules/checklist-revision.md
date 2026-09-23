# Checklist de revisión — Bancos

Lo aplica el skill global `project-code-review` sobre cada diff, además de sus verificaciones generales.

## Checklist

* [ ] Conciliación correcta.
* [ ] Idempotencia de importación.
* [ ] Preservación de clasificaciones.
* [ ] Moneda correcta y regeneración FX.
* [ ] Sin secretos ni datos financieros (movimientos, saldos, cuentas, archivos fuente) en logs ni en `/ia`.
* [ ] El cambio no contradice un ADR vigente.
