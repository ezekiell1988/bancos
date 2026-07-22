# TASK-EBC-DB-07 — Agregar tablas transaccionales al MCP: tbPeriods, tbTransactions, tbCardStatements, tbCardFinancings, tbLoanStatements y tbLoanPayments

**Estado:** Borrador
**Autor:** Ezequiel Baltodano Cubillo `<pendiente>`
**Rama:** `dev`
**Fecha inicio:** 2026-07-21 09:20 CR
**Fecha cierre:** —
**Área:** DB
**Prioridad:** alta
**Riesgo:** medio
**Aprobación:** pendiente

---

## Título

Agregar tablas transaccionales al MCP: tbPeriods, tbTransactions, tbCardStatements, tbCardFinancings, tbLoanStatements y tbLoanPayments

## Contexto

La BD del MCP (Bancos.Mcp) ya tiene el catálogo base: tbBanks, tbBankAccounts, tbImportTemplates, tbImportTemplatePatterns, tbBankAccountImportTemplates y tbExchangeRates. Lo que falta son las tablas transaccionales donde se persistirán los datos extraídos de los archivos importados.

**Períodos de reporte:** el usuario sube archivos de movimientos agrupados por período para revisarlos antes del pago. Cada período corre del 19 de un mes al 18 del siguiente (ciclo de corte BAC). tbPeriods es una tabla global exclusiva de tbTransactions — no está vinculada a ninguna otra tabla.

**Corte de tarjeta (tbCardStatements):** es una tabla independiente del período. El período aparece solo como dato informativo en el header del estado de cuenta (ej. "JUL-2026") pero no hay FK hacia tbPeriods. tbCardStatements se vincula a tbTransactions mediante idCardStatements (nullable FK en tbTransactions): primero se insertan los movimientos, luego opcionalmente se sube el PDF del estado de cuenta.

La convención de la BD es camelCase con prefijo tb, PK idNombreTabla, FK idTablaReferenciada, timestamps datetimeoffset.

## Objetivo

Crear las 7 tablas transaccionales en Bancos.Mcp con sus entidades de dominio, configuraciones Fluent API y migración EF Core, siguiendo la convención ya establecida en el proyecto.

## Alcance permitido

* Bancos.Mcp/Domain — nuevas entidades
* Bancos.Mcp/Data/McpCatalogDbContext.cs — DbSets y configuraciones
* Bancos.Mcp/Migrations/ — nueva migración

## Fuera de alcance

* Parsers que poblen estas tablas
* Endpoints o tools MCP que expongan los datos
* Lógica de P&L

## Criterios de aceptación

* [ ] Las 7 tablas existen en la BD MCP tras aplicar la migración (tbPeriods, tbTransactions, tbCardStatements, tbCardStatementLines, tbCardFinancings, tbLoanStatements, tbLoanPayments)
* [ ] Convención camelCase con prefijo tb aplicada en todos los nombres
* [ ] UNIQUE constraints en sourceFingerprint por tabla
* [ ] tbLoanPayments.idLoanStatements tiene FK con CASCADE
* [ ] tbCardStatementLines tiene surrogate PK idCardStatementLines + UNIQUE (idCardStatements, idTransactions)
* [ ] tbTransactions no tiene FK directa hacia tbCardStatements
* [ ] tbTransactions.idPeriods tiene FK nullable hacia tbPeriods
* [ ] tbCardStatements no tiene FK hacia tbPeriods — solo campo informativo statementPeriodLabel
* [ ] dbQuery puede hacer SELECT en las 6 tablas sin error

## Riesgos

Riesgo medio.

## Archivos afectados / probables

* `src/Bancos.Mcp/Domain/Period.cs`
* `src/Bancos.Mcp/Domain/Transaction.cs`
* `src/Bancos.Mcp/Domain/CardStatement.cs`
* `src/Bancos.Mcp/Domain/CardFinancing.cs`
* `src/Bancos.Mcp/Domain/LoanStatement.cs`
* `src/Bancos.Mcp/Data/McpCatalogDbContext.cs`
* `src/Bancos.Mcp/Migrations/`

## Plan técnico

1. ## tbPeriods — Períodos de reporte (global)

Tabla global de períodos. Un período corre del día 19 del mes anterior al 18 del mes en curso, siguiendo el ciclo de corte de las tarjetas BAC. No está vinculada a ninguna cuenta — es la unidad de tiempo para reportería.

| Columna | Tipo SQL | Notas |
|---|---|---|
| idPeriods | uniqueidentifier PK | |
| label | nvarchar(20) | Ej. "JUL-2026" — nombre visible del período |
| startDate | date | Inicio del período (ej. 2026-06-19) |
| endDate | date | Fin del período / fecha de corte (ej. 2026-07-18) |
| createdAt | datetimeoffset | |
| updatedAt | datetimeoffset nullable | |

Índices: UNIQUE (startDate), UNIQUE (endDate), UNIQUE (label)

2. ## tbTransactions — Movimientos individuales de cuenta o tarjeta

Una fila por movimiento extraído de cualquier formato (CSV BCR, XLS movimientos, PDF BAC, etc.).

| Columna | Tipo SQL | Notas |
|---|---|---|
| idTransactions | uniqueidentifier PK | |
| idBankAccounts | uniqueidentifier FK→tbBankAccounts | |
| idPeriods | uniqueidentifier FK→tbPeriods nullable | período de reporte; null si aún no se ha creado el período |
| referenceNumber | nvarchar(40) nullable | N. Referencia del extracto |
| transactionDate | date | Fecha de la transacción |
| paymentDate | date nullable | Fecha de pago (si aplica) |
| description | nvarchar(200) | Concepto/Descripción |
| place | nvarchar(120) nullable | Lugar / comercio |
| currencyCode | nchar(3) | CK: IN ('CRC','USD') |
| amount | decimal(18,2) | positivo=cargo, negativo=abono |
| amountCrc | decimal(18,2) | monto en CRC (=amount si CRC, convertido si USD) |
| exchangeRate | decimal(18,6) nullable | tipo de cambio usado |
| operationType | nvarchar(32) | CK: IN ('purchase','payment','interest','other-charge','interest-reversal') |
| sourceFingerprint | nchar(64) | SHA-256 para deduplicación |
| createdAt | datetimeoffset | |
| updatedAt | datetimeoffset nullable | |

Índices: UNIQUE (idBankAccounts, sourceFingerprint)

2. ## tbCardStatements — Corte mensual de tarjeta de crédito

Header del estado de cuenta mensual con los totales del período. Se crea al subir el PDF del corte. La asociación con los movimientos individuales se maneja en la tabla auxiliar tbCardStatementLines (no hay FK directa en tbTransactions).

| Columna | Tipo SQL | Notas |
|---|---|---|
| idCardStatements | uniqueidentifier PK | |
| idBankAccounts | uniqueidentifier FK→tbBankAccounts | |
| statementDate | date | Fecha de corte (ej. 2026-07-18) |
| periodLabel | nvarchar(20) | Período informativo del header (ej. "JUL-2026") |
| minimumPaymentDueDate | date nullable | Fecha límite pago mínimo |
| cashPaymentDueDate | date nullable | Fecha límite pago de contado |
| previousBalanceCrc | decimal(18,2) | Saldo anterior CRC |
| previousBalanceUsd | decimal(18,2) | Saldo anterior USD |
| purchasesTotalCrc | decimal(18,2) | Total compras CRC |
| purchasesTotalUsd | decimal(18,2) | Total compras USD |
| paymentsTotalCrc | decimal(18,2) | Total pagos recibidos CRC |
| paymentsTotalUsd | decimal(18,2) | Total pagos recibidos USD |
| interestTotalCrc | decimal(18,2) | Total intereses CRC |
| interestTotalUsd | decimal(18,2) | Total intereses USD |
| currentBalanceCrc | decimal(18,2) | Saldo actual CRC |
| currentBalanceUsd | decimal(18,2) | Saldo actual USD |
| minimumPaymentCrc | decimal(18,2) | Pago mínimo CRC |
| minimumPaymentUsd | decimal(18,2) | Pago mínimo USD |
| cashPaymentCrc | decimal(18,2) | Pago de contado CRC |
| cashPaymentUsd | decimal(18,2) | Pago de contado USD |
| creditLimitCrc | decimal(18,2) | Límite de crédito CRC |
| creditLimitUsd | decimal(18,2) | Límite de crédito USD |
| availableBalanceCrc | decimal(18,2) | Saldo disponible CRC |
| availableBalanceUsd | decimal(18,2) | Saldo disponible USD |
| sourceFingerprint | nchar(64) | SHA-256 para deduplicación |
| createdAt | datetimeoffset | |
| updatedAt | datetimeoffset nullable | |

Índices: UNIQUE (idBankAccounts, statementDate)

3. ## tbCardStatementLines — Auxiliar: movimientos incluidos en un corte

Tabla de unión entre tbCardStatements y tbTransactions. Una fila por movimiento asociado a un corte. Usa surrogate PK propia + UNIQUE constraint en lugar de PK compuesta — ver decisión de diseño en `06_decisions/ADR-03-surrogate-pk-junction-tables.md`.

| Columna | Tipo SQL | Notas |
|---|---|---|
| idCardStatementLines | uniqueidentifier PK | surrogate key |
| idCardStatements | uniqueidentifier FK→tbCardStatements CASCADE | |
| idTransactions | uniqueidentifier FK→tbTransactions RESTRICT | |
| createdAt | datetimeoffset | |

UNIQUE: (idCardStatements, idTransactions) — evita duplicados
Índices: IX (idTransactions) para búsqueda inversa

5. ## tbCardFinancings — Planes de cuotas / tasa cero BAC

Una fila por plan de financiamiento activo en la tarjeta. Solo snapshot del estado actual; no tiene tabla de amortización porque el extracto BAC no la incluye.

| Columna | Tipo SQL | Notas |
|---|---|---|
| idCardFinancings | uniqueidentifier PK | |
| idBankAccounts | uniqueidentifier FK→tbBankAccounts | |
| referenceNumber | nvarchar(40) nullable | |
| financingDate | date | Fecha del financiamiento |
| concept | nvarchar(200) | Descripción del plan |
| currencyCode | nchar(3) | CK: IN ('CRC','USD') |
| initialBalance | decimal(18,2) | Saldo inicial del plan |
| outstandingBalance | decimal(18,2) | Saldo faltante a la fecha del corte |
| installments | nvarchar(20) | Cuotas en formato texto (ej. '3/12') |
| installmentAmount | decimal(18,2) | Monto de cada cuota |
| termMonths | smallint nullable | Plazo total en meses |
| annualInterestRate | decimal(8,4) nullable | Tasa de interés anual (null si tasa cero) |
| dueDate | date nullable | Fecha de vencimiento del plan |
| status | nvarchar(16) | CK: IN ('active','cancelled','settled') |
| sourceFingerprint | nchar(64) | SHA-256 para deduplicación |
| createdAt | datetimeoffset | |
| updatedAt | datetimeoffset nullable | |

Índices: UNIQUE (idBankAccounts, sourceFingerprint)

6. ## tbLoanStatements — Encabezado del préstamo (Coopealianza y futuros)

Una fila por extracto de préstamo importado. Es el padre de tbLoanPayments.

| Columna | Tipo SQL | Notas |
|---|---|---|
| idLoanStatements | uniqueidentifier PK | |
| idBankAccounts | uniqueidentifier FK→tbBankAccounts | accountType='loan' |
| statementDate | date | Fecha del extracto |
| currencyCode | nchar(3) | CK: IN ('CRC','USD') |
| outstandingBalance | decimal(18,2) | Saldo pendiente total |
| sourceFingerprint | nchar(64) | SHA-256 para deduplicación |
| createdAt | datetimeoffset | |
| updatedAt | datetimeoffset nullable | |

Índices: UNIQUE (idBankAccounts, sourceFingerprint)

7. ## tbLoanPayments — Filas del calendario de pagos

Una fila por cuota programada dentro de un extracto. FK al tbLoanStatements padre.

| Columna | Tipo SQL | Notas |
|---|---|---|
| idLoanPayments | uniqueidentifier PK | |
| idLoanStatements | uniqueidentifier FK→tbLoanStatements CASCADE | |
| paymentDate | date | Fecha de la cuota |
| capital | decimal(18,2) | Abono a capital |
| interest | decimal(18,2) | Interés de la cuota |
| lateFee | decimal(18,2) | Mora |
| otherCharges | decimal(18,2) | Otros cargos |
| total | decimal(18,2) | Total de la cuota |
| balance | decimal(18,2) | Saldo después del pago |
| sourceFingerprint | nchar(64) | SHA-256 para deduplicación |
| createdAt | datetimeoffset | |

Índices: UNIQUE (idLoanStatements, sourceFingerprint)

## Pasos

1. Revisar y aprobar propuesta de tablas
2. Crear entidades C# en Bancos.Mcp/Domain
3. Registrar DbSets en McpCatalogDbContext y configurar Fluent API
4. Generar migración EF Core
5. Aplicar migración en local y verificar con dbQuery
6. Actualizar tarea como completada

## Salida esperada

7 tablas nuevas en la BD del MCP listas para recibir datos de los parsers de importación.

## Validación

* [ ] SELECT TOP 1 en cada tabla sin error
* [ ] INFORMATION_SCHEMA.TABLE_CONSTRAINTS confirma UNIQUE y FK
* [ ] INFORMATION_SCHEMA.COLUMNS confirma tipos y precisiones

## Rollback

ef migrations remove y revertir entidades/DbContext. No hay datos en producción.

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
