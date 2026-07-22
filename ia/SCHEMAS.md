# /ia — Esquemas de reconstrucción

Usar solo para crear o reparar contexto. No incluir datos sensibles.

## Tablas transaccionales MCP (TASK-EBC-DB-07)

Creadas el 2026-07-21. Una sola migración `InitialMcpCatalog`. Convención: prefijo `tb`, PK `idNombreTabla`, FK `idTablaReferenciada`, timestamps `datetimeoffset`, columnas en camelCase.

### tbPeriods
| Columna | Tipo | Notas |
|---|---|---|
| idPeriods | uniqueidentifier PK | |
| label | nvarchar(20) | UNIQUE. Ej. "JUL-2026" |
| startDate | date | UNIQUE. Inicio del período (día 19 mes anterior) |
| endDate | date | UNIQUE. Fin del período (día 18 mes en curso) |
| createdAt / updatedAt | datetimeoffset | |

Seed: ENE-2026 a DIC-2026 (IDs `60000000-...-000000000001` a `...000000000012`).

### tbTransactions
| Columna | Tipo | Notas |
|---|---|---|
| idTransactions | uniqueidentifier PK | |
| idBankAccounts | uniqueidentifier FK→tbBankAccounts RESTRICT | |
| idPeriods | uniqueidentifier FK→tbPeriods SET NULL **nullable** | null hasta que exista el período |
| referenceNumber | nvarchar(40) nullable | |
| transactionDate | date | |
| paymentDate | date nullable | |
| description | nvarchar(200) | |
| place | nvarchar(120) nullable | |
| currencyCode | nchar(3) | CK: IN ('CRC','USD') |
| amount | decimal(18,2) | positivo=cargo, negativo=abono |
| amountCrc | decimal(18,2) | convertido a CRC |
| exchangeRate | decimal(18,6) nullable | |
| operationType | nvarchar(32) | CK: IN ('purchase','payment','interest','other-charge','interest-reversal') |
| sourceFingerprint | nchar(64) | UNIQUE con idBankAccounts |
| createdAt / updatedAt | datetimeoffset | |

### tbCardStatements
| Columna | Tipo | Notas |
|---|---|---|
| idCardStatements | uniqueidentifier PK | |
| idBankAccounts | uniqueidentifier FK→tbBankAccounts RESTRICT | |
| statementDate | date | UNIQUE con idBankAccounts |
| periodLabel | nvarchar(20) | Informativo del header, sin FK |
| minimumPaymentDueDate / cashPaymentDueDate | date nullable | |
| previousBalance / purchasesTotal / paymentsTotal / interestTotal / currentBalance / minimumPayment / cashPayment / creditLimit / availableBalance | decimal(18,2) ×2 (Crc/Usd) | |
| sourceFingerprint | nchar(64) | |
| createdAt / updatedAt | datetimeoffset | |

### tbCardStatementLines  _(ADR-03)_
| Columna | Tipo | Notas |
|---|---|---|
| idCardStatementLines | uniqueidentifier PK | Surrogate key |
| idCardStatements | uniqueidentifier FK→tbCardStatements CASCADE | |
| idTransactions | uniqueidentifier FK→tbTransactions RESTRICT | |
| createdAt | datetimeoffset | |

UNIQUE: (idCardStatements, idTransactions). IX en idTransactions para búsqueda inversa.

### tbCardFinancings
| Columna | Tipo | Notas |
|---|---|---|
| idCardFinancings | uniqueidentifier PK | |
| idBankAccounts | uniqueidentifier FK→tbBankAccounts RESTRICT | |
| referenceNumber | nvarchar(40) nullable | |
| financingDate | date | |
| concept | nvarchar(200) | |
| currencyCode | nchar(3) | CK: IN ('CRC','USD') |
| initialBalance / outstandingBalance / installmentAmount | decimal(18,2) | |
| installments | nvarchar(20) | Ej. "3/12" |
| termMonths | smallint nullable | |
| annualInterestRate | decimal(8,4) nullable | null si tasa cero |
| dueDate | date nullable | |
| status | nvarchar(16) | CK: IN ('active','cancelled','settled') |
| sourceFingerprint | nchar(64) | UNIQUE con idBankAccounts |
| createdAt / updatedAt | datetimeoffset | |

### tbLoanStatements
| Columna | Tipo | Notas |
|---|---|---|
| idLoanStatements | uniqueidentifier PK | |
| idBankAccounts | uniqueidentifier FK→tbBankAccounts RESTRICT | accountType='loan' |
| statementDate | date | |
| currencyCode | nchar(3) | CK: IN ('CRC','USD') |
| outstandingBalance | decimal(18,2) | |
| sourceFingerprint | nchar(64) | UNIQUE con idBankAccounts |
| createdAt / updatedAt | datetimeoffset | |

### tbLoanPayments
| Columna | Tipo | Notas |
|---|---|---|
| idLoanPayments | uniqueidentifier PK | |
| idLoanStatements | uniqueidentifier FK→tbLoanStatements CASCADE | |
| paymentDate | date | |
| capital / interest / lateFee / otherCharges / total / balance | decimal(18,2) | |
| sourceFingerprint | nchar(64) | UNIQUE con idLoanStatements |
| createdAt | datetimeoffset | |

---

## Convenciones de SQL Server

Las tablas del catálogo MCP inician con `tb` y usan lower camel case, por ejemplo,
`tbImportTemplates`. Sus columnas también usan lower camel case. Las claves
primarias y foráneas expresan la entidad relacionada, por ejemplo,
`idImportTemplates` e `idBankAccounts`; no se usa una columna SQL genérica `Id`.

Cada tabla y cada columna nueva debe declarar un comentario en español mediante
`HasComment(...)` de EF Core. Los nombres de tablas y columnas se conservan en
inglés. La migración debe incluir esos metadatos para que SQL Server los publique
como descripciones del esquema.

## Componentes `00` a `08`

* `00_context.md`: identidad, stack, límites, mapa y validación.
* `01_requirements.md`: requisitos observables, reglas e ítems fuera de alcance.
* `02_architecture.md`: componentes, flujo, contratos y seguridad.
* `03_plan.md`: fases, hitos, dependencias y tareas vinculadas.
* `04_tasks.md`: índice; cada tarea tiene objetivo, alcance, exclusiones, aceptación, riesgo, aprobación, validación y rollback.
* `05_progress.md`: puntero a estado actual e historial.
* `06_decisions.md`: solo índice; un archivo por ADR.
* `07_issues.md`: índice; detalle separado por issue.
* `08_retrospective.md`: aprendizajes accionables por fase.

## Tarea

```markdown
# TASK-{INICIALES}-{ÁREA}-{NN} — {título}
> Estado: Borrador | Lista | En progreso | Bloqueada | En revisión | Completada
> Riesgo: Bajo | Medio | Alto
> Aprobación: Pendiente | Aprobada explícitamente
## Contexto
## Objetivo
## Incluye
## No incluye
## Criterios de aceptación
## Plan técnico
## Validación
## Rollback
```

Tareas de riesgo alto no avanzan sin aprobación explícita.
