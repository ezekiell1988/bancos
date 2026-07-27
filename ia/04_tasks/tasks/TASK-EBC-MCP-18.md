# TASK-EBC-MCP-18 — Cierres de saldo por periodo por cuenta bancaria

**Estado:** Lista
**Autor:** Ezequiel Baltodano Cubillo
**Rama:** `dev`
**Fecha inicio:** 2026-07-24
**Fecha cierre:** —
**Área:** MCP
**Prioridad:** media
**Riesgo:** bajo
**Aprobación:** aprobada

---

## Contexto

El sistema persiste movimientos en `tbTransactions` y periodos en `tbPeriods`. Se necesita materializar el **saldo acumulado por cuenta y periodo** para consulta rápida por el frontend y herramientas MCP.

El saldo inicial de una cuenta se ingresa manualmente como un movimiento especial en `tbTransactions` (descripción: "Saldo inicial") en el periodo más antiguo disponible. No se requiere columna separada para saldo de apertura.

### Modelo de datos

```
balance(periodo N) = balance(periodo N-1) + SUM(tbTransactions del periodo N)
balance(periodo 0) = SUM(tbTransactions del primer periodo)  -- incluye movimiento "Saldo inicial"
```

Solo se persiste `balance` (saldo al cierre del periodo). No se almacena saldo de apertura ni movimientos como columnas separadas porque son derivables.

## Objetivo

Crear la tabla `tbAccountPeriodClosings`, el endpoint que calcula y persiste los cierres desde un periodo dado hacia adelante, y el MCP tool que encola ese proceso como Hangfire job.

## Alcance permitido

* Crear feature `AccountPeriodClosings` en `Bancos.Mcp`.
* Crear entidad EF `AccountPeriodClosing` y migración.
* Implementar `CalculateAccountPeriodClosingsJob` (Hangfire).
* Implementar endpoint `POST /account-period-closings/calculate`.
* Implementar MCP tool `calculate_period_closings`.
* Agregar `DbSet` en `BancosDbContext`.

## Fuera de alcance

* Modificar `tbTransactions`, `tbPeriods` ni ninguna otra tabla existente.
* Conversión USD/CRC (se usa `AmountCrc` directamente).
* Frontend o visualización de cierres.
* Endpoint de consulta de cierres (solo cálculo en esta tarea).

## Pasos

1. Crear entidad `AccountPeriodClosing` con FK a `tbBankAccounts` y `tbPeriods`.
2. Agregar `DbSet<AccountPeriodClosing>` en `BancosDbContext`.
3. Regenerar migración inicial `InitialCreate` (Bancos.Mcp usa una única migración).
4. Aplicar migración en BD local.
5. Implementar `CalculateAccountPeriodClosingsJob`:
   - Recibe `periodId`.
   - Obtiene todos los periodos desde ese `periodId` en adelante (ordenados).
   - Para cada cuenta con movimientos en alguno de esos periodos:
     - Obtiene balance del periodo anterior (de `tbAccountPeriodClosings` o 0).
     - Calcula `movements = SUM(AmountCrc)` de `tbTransactions` del periodo.
     - `balance = previousBalance + movements`.
     - Upsert en `tbAccountPeriodClosings`.
6. Implementar endpoint `POST /account-period-closings/calculate` que encola el job.
7. Implementar MCP tool `calculate_period_closings` que recibe `periodId` y encola el job.

## Salida esperada

* Tabla `tbAccountPeriodClosings` creada en BD local.
* Endpoint funcional: `POST /account-period-closings/calculate` encola job y retorna `jobId`.
* MCP tool `calculate_period_closings` disponible en `tools/list`.
* Job Hangfire en estado `Succeeded` tras ejecutarse.
* `dotnet build` sin errores y 46/46 tests pasan.

## Criterios de aceptación

* [ ] Migración aplicada: tabla `tbAccountPeriodClosings` existe en BD local.
* [ ] Endpoint calcula y persiste cierres desde el periodo indicado hacia adelante.
* [ ] Re-ejecución del endpoint hace upsert (no duplica filas).
* [ ] MCP tool `calculate_period_closings` encola el job correctamente.
* [ ] Job Hangfire aparece en estado `Succeeded` tras ejecutarse.
* [ ] `dotnet build` sin errores y tests existentes pasan (46/46).

## Archivos probables

* `src/Bancos.Mcp/Migrations/` ← migración EF regenerada
* `src/Bancos.Mcp/Features/AccountPeriodClosings/` ← feature nueva
  * `AccountPeriodClosing.cs`
  * `AccountPeriodClosingsEndpoints.cs`
  * `CalculateAccountPeriodClosingsJob.cs`
  * `CalculatePeriodClosingsTool.cs`
* `src/Bancos.Mcp/Data/BancosDbContext.cs`

## Validación

* [ ] Migración aplicada sin errores.
* [ ] Job Succeeded en Hangfire Dashboard.
* [ ] Consulta directa a `tbAccountPeriodClosings` muestra saldos coherentes con los movimientos.
* [ ] `dotnet build` limpio y 46/46 tests pasan.

## Rollback

Revertir migración con `dotnet ef database update <migración-anterior>` y eliminar archivos de la feature.

## Dependencias

* `tbTransactions` — fuente de movimientos; ya implementada.
* `tbPeriods` — periodos de corte; ya implementada.
* `tbBankAccounts` — cuentas bancarias; ya implementada.

## Issues vinculados

* ninguno

## Notas / contexto adicional

* Aprobada por EBC el 2026-07-24 17:42 CR.
