# 02 — Arquitectura del Sistema

> Última actualización: 2026-07-21

## Resumen

**Dirección estratégica:** migrar toda la funcionalidad de `Bancos.Api` a `Bancos.Mcp`
de forma progresiva. `Bancos.Api` está en retiro; `Bancos.Mcp` es el proyecto destino
y será el único proyecto activo al completar la migración.

`Bancos.Api` es el monolito funcional original (en retiro progresivo): recibe
importaciones, ejecuta jobs Hangfire, persiste MSSQL y sirve el frontend Angular.

`Bancos.Mcp` es el servidor MCP destino: acumula tools equivalentes a cada feature
de la API. Usa `McpCatalogDbContext` y su propia base de datos (`dbbancosmcp`).

## Límites de proyectos y datos

`Bancos.Api` y `Bancos.Mcp` tienen bases de datos, cadenas de conexión, contextos y
migraciones de EF Core distintos. No comparten tablas ni `__EFMigrationsHistory`.
Las migraciones de un proyecto se aplican únicamente contra su propia base de datos.

Las tablas del catálogo MCP usan el prefijo `tb` y lower camel case. Sus columnas
usan lower camel case, con claves descriptivas como `idBanks` e
`idImportTemplates`. EF Core publica comentarios en español para cada tabla y
columna; los nombres físicos permanecen en inglés.

## Features

| Feature | Responsabilidad |
|---|---|
| `Imports` | Upload temporal, huella, detección de plantilla y encolado. |
| `Parsing` | Extractores PDF/Excel/CSV por firma de formato. |
| `Ledger` | Cuentas, auxiliares, movimientos y comprobantes. |
| `Classification` | Reglas, categorías, tags, IA y revisión manual. |
| `ExchangeRates` | Tipo de cambio diario y fallback. |
| `ForeignExchange` | Cierres regenerables de pasivos USD. |
| `Reports` | Pérdidas/ganancias y situación financiera. |
| `CardStatements` | Cortes de tarjetas, período, vencimiento y pago agrupado analítico. |

## Flujo de importación

```text
[Upload] -> [ImportId] -> [Hangfire job]
 -> detectar plantilla -> extraer -> validar saldo
 -> crear/actualizar movimientos -> clasificar
 -> contabilizar -> reportar resultado
```

Job recibe solo `ImportId`; bytes no viajan en argumentos Hangfire. Console registra inicio, plantilla, extracción, conciliación, clasificación, persistencia y fallo/éxito.

## Reglas de clasificación

1. Misma cuenta y descripción normalizada: última clasificación aprobada.
2. Regla determinística por patrón/tags.
3. Azure AI con descripción y catálogo; sin archivo, identificador ni saldo.
4. Categoría previamente asignada si descripción cambió.
5. `General`, pendiente de revisión manual.

## Datos esenciales

`Accounts`, `AccountAuxiliaries`, `Imports`, `ImportFingerprints`, `Transactions`, `JournalEntries`, `JournalLines`, `Categories`, `ClassificationRules`, `ClassificationTags`, `ExchangeRates`, `ForeignExchangeClosings`, `ForeignExchangeClosingLines`.

`AccountAuxiliaries.Iban` es llave de negocio única cuando existe: identificador `CR...` normalizado, no número de tarjeta. `OwnerId` se resuelve desde documento; el fallback acordado es Ezequiel Baltodano.

Los períodos contables parten del 2025-12-31. `CardStatements` mantiene ciclos del banco y sus movimientos vinculados; el cierre de tarjeta informa pago y liquidez, mientras el libro mayor registra compras/pagos por fecha contable.

## Tablas transaccionales del MCP

El catálogo MCP incluye, además de las tablas de catálogo base (`tbBanks`, `tbBankAccounts`, `tbImportTemplates`, `tbImportTemplatePatterns`, `tbBankAccountImportTemplates`, `tbExchangeRates`), las siguientes tablas transaccionales creadas en `TASK-EBC-DB-07`:

### Períodos (`tbPeriods`)

Tabla global de períodos de reporte. Un período corre del día **19 del mes anterior** al **18 del mes en curso**, siguiendo el ciclo de corte BAC. No está vinculada a ninguna cuenta ni a los cortes de tarjeta — es exclusivamente el eje de tiempo para agrupar movimientos antes del pago. Sembrados: ENE-2026 a DIC-2026.

### Movimientos (`tbTransactions`)

Un registro por movimiento extraído de cualquier formato de importación. `idPeriods` es nullable: el período se asigna cuando existe; si se importa antes de crear el período queda null. No tiene FK directa hacia `tbCardStatements` — la asociación se gestiona por `tbCardStatementLines`.

### Corte de tarjeta (`tbCardStatements` + `tbCardStatementLines`)

`tbCardStatements` guarda el header con todos los totales del período (saldo anterior, compras, pagos, intereses, saldo actual, pago mínimo y de contado, límite y disponible, en CRC y USD). `periodLabel` es un campo informativo extraído del PDF; no es FK.

`tbCardStatementLines` es la tabla auxiliar que asocia movimientos a un corte: surrogate PK (`idCardStatementLines`) + UNIQUE `(idCardStatements, idTransactions)`. Ver **ADR-03** para la decisión de diseño.

**Flujo de carga:** primero se insertan los movimientos en `tbTransactions`, luego se crea el registro en `tbCardStatements` y se pobla `tbCardStatementLines`.

### Financiamientos de tarjeta (`tbCardFinancings`)

Snapshot de los planes de cuotas/tasa cero activos en una tarjeta BAC. Una fila por plan; no tiene tabla de amortización porque el extracto BAC no la incluye.

### Préstamos (`tbLoanStatements` + `tbLoanPayments`)

`tbLoanStatements` es el encabezado del extracto de préstamo (Coopealianza y futuros). `tbLoanPayments` contiene las filas del calendario de amortización: capital, interés, mora, otros, total y saldo por cuota. FK con CASCADE desde cuota hacia encabezado.

### Diagrama de relaciones

```
tbBanks ──< tbBankAccounts >──< tbBankAccountImportTemplates >── tbImportTemplates
                │                                                       │
                │                                            tbImportTemplatePatterns
                ├──< tbTransactions >──< tbCardStatementLines >── tbCardStatements
                │         │
                │    tbPeriods (nullable)
                │
                ├──< tbCardFinancings
                │
                └──< tbLoanStatements ──< tbLoanPayments
```

## Formatos de importación

Firmas, campos y reglas de detección: [`02_architecture/import-formats.md`](02_architecture/import-formats.md). Nunca usar nombre de archivo ni ruta como criterio de negocio.

## Seguridad

Archivo `db.json` se carga solo desde configuración local; nunca se devuelve por API ni se versiona. API local no tiene auth; publicación requiere ADR y tarea de autenticación.
