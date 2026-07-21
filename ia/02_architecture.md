# 02 — Arquitectura del Sistema

> Última actualización: 2026-07-18

## Resumen

`Bancos.Api` es el monolito modular funcional: Angular es UI; la API recibe
importaciones, ejecuta jobs, persiste MSSQL y sirve frontend compilado.

`Bancos.Mcp` es un servidor MCP auxiliar independiente del monolito. Mantiene el
catálogo de plantillas de importación mediante `McpCatalogDbContext` y no participa
en los flujos contables, los jobs ni la persistencia funcional de la API.

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

## Formatos de importación

Firmas, campos y reglas de detección: [`02_architecture/import-formats.md`](02_architecture/import-formats.md). Nunca usar nombre de archivo ni ruta como criterio de negocio.

## Seguridad

Archivo `db.json` se carga solo desde configuración local; nunca se devuelve por API ni se versiona. API local no tiene auth; publicación requiere ADR y tarea de autenticación.
