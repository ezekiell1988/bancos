# TASK-EBC-FE-09 — Página de Estado de Resultados mensual

**Estado:** Borrador
**Autor:** Ezequiel Baltodano Cubillo `<pendiente>`
**Rama:** `dev`
**Fecha inicio:** 2026-07-20 19:33 CR
**Fecha cierre:** —
**Área:** FE
**Prioridad:** alta
**Riesgo:** bajo
**Aprobación:** pendiente

---

## Título

Página de Estado de Resultados mensual

## Contexto

El propósito principal del sistema es generar estados mensuales de pérdidas y ganancias para la Familia Baltodano Soto. Transacciones ya clasificadas en BD. Moneda funcional CRC; USD tiene AmountCrc convertido. Accounts con Kind: Asset/Liability/Income/Expense.

## Objetivo

Crear ruta /reports/income-statement con selector de mes/año, tabla de ingresos vs gastos agrupados por categoría, totales netos en CRC y resultado del período (superávit/déficit).

## Alcance permitido

* src/Bancos.Web/src/app/features/reports/income-statement/
* src/Bancos.Web/src/app/app.routes.ts
* src/Bancos.Api/Features/Reports/

## Fuera de alcance

* Exportación PDF/Excel
* Comparativo multi-mes
* Gráficos
* JournalEntries

## Criterios de aceptación

* [ ] Selector mes/año carga el estado del período seleccionado
* [ ] Ingresos y gastos separados, agrupados por categoría con subtotal
* [ ] Resultado neto del período visible (supéravit en verde, déficit en rojo)
* [ ] Ruta /reports/income-statement registrada

## Riesgos

Riesgo bajo.

## Archivos afectados / probables

* `pendiente de confirmar`

## Plan técnico

1. Endpoint GET /api/reports/income-statement?year=&month= que agrupa Transactions por OperationType/Categoría
2. DTO de respuesta: { ingresos: [{category, total}], gastos: [{category, total}], neto }
3. Feature Angular reports con IncomeStatementPage y ReportsApi
4. Registrar rutas /reports/* en app.routes.ts

## Pasos

1. Crear ReportsModule BE con endpoint income-statement
2. Crear feature FE reports/income-statement
3. Registrar ruta en app.routes.ts
4. Verificar con datos de julio 2026

## Salida esperada

Página /reports/income-statement mostrando ingresos, gastos y neto del mes seleccionado

## Validación

* [ ] GET /api/reports/income-statement?year=2026&month=7 devuelve totales correctos
* [ ] Ingresos y gastos cuadran con suma manual de Transactions
* [ ] Cambio de mes actualiza la vista

## Rollback

Eliminar ruta de app.routes.ts y el feature reports; el endpoint BE es read-only

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
