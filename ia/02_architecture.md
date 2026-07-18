# 02 — Arquitectura del Sistema

> Última actualización: 2026-07-18

## Resumen

Monolito modular: Angular es UI; .NET recibe importaciones, ejecuta jobs, persiste MSSQL y sirve frontend compilado. No existen dos backends.

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
