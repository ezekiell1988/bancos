# TASK-EBC-FE-11 — Página de Préstamos y financiamientos

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

Página de Préstamos y financiamientos

## Contexto

Los datos de préstamos están en LoanStatements/LoanPayments (Coopealianza) y los financiamientos de tarjeta en CreditFinancings (BAC tasa cero y cuotas). El usuario ya consultó estos datos manualmente via SQL para saber cuánto paga mensual. Necesita una página dedicada.

## Objetivo

Crear ruta /loans con: sección de financiamientos BAC (CreditFinancings agrupados por concepto con cuota mensual y saldo faltante), sección de préstamos Coopealianza (LoanStatements con cuota, saldo y próximo pago), y total de pago fijo mensual en CRC.

## Alcance permitido

* src/Bancos.Web/src/app/features/loans/
* src/Bancos.Web/src/app/app.routes.ts
* src/Bancos.Api/Features/Loans/ (nuevo o existente)

## Fuera de alcance

* Simulación de amortización
* Calendario de pagos futuro
* Importación desde esta vista

## Criterios de aceptación

* [ ] Financiamientos BAC listados con concepto, cuota CRC/USD y saldo faltante
* [ ] Préstamos Coopealianza con cuota total y saldo vigente
* [ ] Total de pago fijo mensual consolidado visible en encabezado
* [ ] Ruta /loans registrada en app.routes.ts

## Riesgos

Riesgo bajo.

## Archivos afectados / probables

* `pendiente de confirmar`

## Plan técnico

1. Endpoint GET /api/loans que retorna CreditFinancings activos (SaldoFaltante > 0) + LoanStatements recientes
2. DTO: { financiamientos: [{concepto, cuotaCrc, cuotaUsd, saldoFaltante, currencyCode}], prestamos: [{nombre, cuota, saldo}], totalMensualCrc }
3. Feature Angular standalone loans con LoansApi y LoansPage

## Pasos

1. Crear endpoint GET /api/loans en LoansModule BE
2. Crear feature FE loans con LoansPage
3. Registrar ruta /loans en app.routes.ts
4. Verificar totales contra consulta SQL previa (tasa cero BAC)

## Salida esperada

Página /loans mostrando todos los financiamientos y préstamos con total mensual consolidado

## Validación

* [ ] Total mensual CRC cuadra con cálculo manual previo
* [ ] Financiamientos USD muestran equivalente en CRC al tipo de cambio vigente
* [ ] Préstamos Coopealianza muestran saldo correcto del último estado importado

## Rollback

Eliminar ruta y feature; endpoint BE es read-only

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
