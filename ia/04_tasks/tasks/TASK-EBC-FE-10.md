# TASK-EBC-FE-10 — Página de Estado de Situación Financiera

**Estado:** Borrador
**Autor:** Ezequiel Baltodano Cubillo `<pendiente>`
**Rama:** `dev`
**Fecha inicio:** 2026-07-20 19:34 CR
**Fecha cierre:** —
**Área:** FE
**Prioridad:** alta
**Riesgo:** bajo
**Aprobación:** pendiente

---

## Título

Página de Estado de Situación Financiera

## Contexto

El sistema tiene AccountAuxiliaries con saldos implícitos en Transactions (activos: BCR), pasivos (tarjetas BAC con CardStatements y CreditFinancings), y préstamos (LoanStatements). Se necesita una vista consolidada de la posición patrimonial en un momento dado.

## Objetivo

Crear ruta /reports/balance-sheet con: sección Activos (saldos de cuentas BCR), sección Pasivos (saldo tarjetas BAC por CardStatement + financiamientos + préstamos), patrimonio neto (activos - pasivos). Todo en CRC con equivalente USD donde aplique.

## Alcance permitido

* src/Bancos.Web/src/app/features/reports/balance-sheet/
* src/Bancos.Web/src/app/app.routes.ts
* src/Bancos.Api/Features/Reports/

## Fuera de alcance

* Exportación
* Comparativo de fechas
* JournalEntries
* Diferencial cambiario

## Criterios de aceptación

* [ ] Activos muestran saldo neto por auxiliar (suma de AmountCrc de Transactions)
* [ ] Pasivos muestran CashPaymentCrc del último CardStatement por tarjeta + saldo de préstamos
* [ ] Patrimonio neto = Activos - Pasivos visible al final
* [ ] Ruta /reports/balance-sheet registrada

## Riesgos

Riesgo bajo.

## Archivos afectados / probables

* `pendiente de confirmar`

## Plan técnico

1. Endpoint GET /api/reports/balance-sheet que consolida: saldo neto Transactions por auxiliar Asset, último CardStatement por tarjeta, saldo faltante CreditFinancings, saldo LoanStatements
2. Feature Angular reports/balance-sheet compartiendo ReportsApi y rutas con FE-09

## Pasos

1. Agregar endpoint balance-sheet a ReportsModule BE
2. Crear BalanceSheetPage en feature reports
3. Registrar ruta /reports/balance-sheet
4. Verificar totales contra consultas BD directas

## Salida esperada

Página /reports/balance-sheet con activos, pasivos y patrimonio neto en CRC

## Validación

* [ ] Saldo de cuenta BCR cuadra con último movimiento importado
* [ ] Pasivos incluyen tarjetas, financiamientos y préstamos
* [ ] Patrimonio neto = total activos - total pasivos

## Rollback

Eliminar ruta y componente; endpoint BE es read-only

## Dependencias

* ninguna

## Checklist

* [ ] Alcance revisado
* [ ] Riesgo revisado
* [ ] Aprobación registrada si aplica
* [ ] Implementación completa
* [ ] Validación completa
* [ ] Progreso/documentación actualizado

## Notas / contexto adicional

Sin notas adicionales.

## Issues vinculados

* ninguno
