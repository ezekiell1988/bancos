# TASK-EBC-BE-25 — Calcular porción corriente y largo plazo del préstamo desde cuotas

**Estado:** Lista
**Autor:** Ezequiel Baltodano Cubillo `<pendiente>`
**Rama:** `dev`
**Fecha inicio:** 2026-07-22 14:37 CR
**Fecha cierre:** —
**Área:** BE
**Prioridad:** alta
**Riesgo:** bajo
**Aprobación:** aprobada

---

## Título

Calcular porción corriente y largo plazo del préstamo desde cuotas

## Contexto

tbLoanPayments ya contiene las 36 cuotas del préstamo Coopealianza con capital, interés, mora, otros y total por cuota. tbLoanStatements tiene el header del préstamo (monto original, tasa, plazo, saldo). Para estados financieros se necesita clasificar la deuda en porción corriente (próximos 12 meses) y largo plazo (después de 12 meses), separando capital e intereses. La moneda es CRC.

## Objetivo

Agregar campos calculados a tbLoanStatements que desglosen lo que se debe pagar en el próximo mes (corto plazo inmediato) y en los próximos 12 meses vs después de 12 meses (porción corriente vs largo plazo), separando capital e intereses.

## Alcance permitido

* Agregar campos a LoanStatement: NextMonthCapital, NextMonthInterest, NextMonthTotal, CurrentPortionCapital, CurrentPortionInterest, CurrentPortionTotal, LongTermCapital, LongTermInterest, LongTermTotal
* Agregar campo CurrencyCode si no está explícito como CRC
* Crear lógica de cálculo que sume cuotas Vigente agrupadas por horizonte temporal
* Actualizar EF migration
* Ejecutar cálculo durante ProcessLoan en ImportFileJob

## Fuera de alcance

* Modificar el parser de PDFs
* UI o reportes
* Multimoneda — todo es CRC por ahora
* Cálculo de diferencial cambiario

## Criterios de aceptación

* [ ] tbLoanStatements contiene NextMonthCapital/Interest/Total con la cuota del próximo mes pendiente
* [ ] tbLoanStatements contiene CurrentPortionCapital/Interest/Total con suma de cuotas pendientes de los próximos 12 meses
* [ ] tbLoanStatements contiene LongTermCapital/Interest/Total con suma de cuotas pendientes después de 12 meses
* [ ] CurrencyCode es CRC
* [ ] Los campos se recalculan en cada procesamiento de PDFs
* [ ] La suma de CurrentPortion + LongTerm = total de cuotas pendientes

## Riesgos

Riesgo bajo.

## Archivos afectados / probables

* `pendiente de confirmar`

## Plan técnico

1. Agregar 9 campos nullable decimal a LoanStatement (3 grupos × 3: Capital, Interest, Total)
2. En ProcessLoan, después de acumular cuotas, filtrar cuotas Vigente y agrupar por horizonte: próximo mes, 1-12 meses, >12 meses desde hoy
3. Sumar Capital, Interest y Total por grupo y asignar a los campos del statement
4. Generar migración EF para los nuevos campos
5. Verificar que al reprocesar los 4 PDFs los campos se calculen correctamente

## Pasos

1. Agregar campos a Domain/LoanStatement.cs
2. Agregar mappings EF en McpCatalogDbContext
3. Generar migración EF
4. Implementar lógica de cálculo en ImportFileJob.ProcessLoan
5. Reprocesar 4 PDFs y verificar valores en BD

## Salida esperada

tbLoanStatements con desglose corriente/largo plazo calculado automáticamente desde las cuotas pendientes.

## Validación

* [ ] Reprocesar 4 PDFs Coopealianza
* [ ] Verificar NextMonth* coincide con la próxima cuota Vigente
* [ ] Verificar CurrentPortion* = suma cuotas Vigente ≤ 12 meses
* [ ] Verificar LongTerm* = suma cuotas Vigente > 12 meses
* [ ] Verificar CurrentPortion + LongTerm = total pendiente

## Rollback

Revertir migración EF y eliminar campos de LoanStatement

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

* Aprobada por Ezequiel Baltodano Cubillo el 2026-07-22 14:45 CR.

Referencia contable: NIC 1 requiere clasificar pasivos en corriente (≤12 meses) y no corriente (>12 meses). El próximo mes se desglosa aparte para flujo de caja inmediato.

## Issues vinculados

* ninguno
