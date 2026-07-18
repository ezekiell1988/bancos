# TASK-EZ-BE-01 — Base .NET, MSSQL e importación regenerable

> Estado: Borrador
> Área: BE/DB
> Prioridad: Alta
> Riesgo: Alto
> Aprobación: Pendiente

## Contexto

`dbbancos` existe vacía. Aplicación será monolito .NET 10 + Angular, con MSSQL, Hangfire y datos financieros sensibles. Importaciones pueden modificar meses previos; cierres son regenerables manualmente.

## Objetivo

Crear fundación backend con configuración .NET estándar, schema migrable y features para importaciones, cuentas/auxiliares, catálogo y jobs de regeneración.

## Incluye

* Solución .NET 10 Minimal API por features y configuración segura.
* `appsettings.json` sin secretos y `appsettings.Development.json` ignorado.
* EF Core/MSSQL, migración inicial y modelo auditado.
* Hangfire SQL Server + Hangfire.Console; jobs reciben IDs pequeños.
* Upload temporal, registro de importación, banderas de períodos pendientes y job de regeneración manual.
* Entidades para IBAN, propietario, auxiliares, moneda, tipo de cambio, asiento, conciliación N:N, auditoría y cierres FX.

## No incluye

* Aplicar migración contra `dbbancos` sin validación explícita del usuario.
* Lectores finales de todas las plantillas, IA Azure, dashboard Angular o autenticación.
* Eliminar registros automáticamente.

## Criterios de aceptación

* API compila, usa `appsettings` y no expone secretos.
* Migración genera schema sin datos financieros y puede revertirse en ambiente vacío.
* Endpoint de upload crea importación y job sin pasar bytes a Hangfire.
* Job de regeneración se encola solo por acción manual, muestra etapas en Console y marca reportes desactualizados hasta terminar.
* Todas las mutaciones generan auditoría; conciliación admite N:N.

## Plan técnico

1. Crear solución y estructura por features.
2. Crear configuración Development ignorada desde secreto local.
3. Modelar entidades, constraints e índices; generar migración sin aplicarla.
4. Configurar Hangfire y endpoints de salud/importación/regeneración.
5. Agregar pruebas de modelo, idempotencia y job.

## Validación

* `dotnet build`, `dotnet test` y revisión de migración.
* No secretos ni movimientos en archivos versionados.
* Aplicación de migración solo tras aprobación separada.

## Rollback

Eliminar solución creada antes de migrar. Si una migración se aplicara por autorización posterior, usar migración descendente o BD vacía de respaldo confirmado.
