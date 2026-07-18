# TASK-EZ-BE-01 — Base .NET, MSSQL e importación regenerable

**Estado:** Lista
**Autor:** Ezequiel Baltodano Cubillo
**Rama:** `main`
**Fecha inicio:** 2026-07-18 12:42 CR
**Fecha cierre:** —
**Área:** BE
**Prioridad:** alta
**Riesgo:** alto
**Aprobación:** aprobada

---

## Título

Base .NET, MSSQL e importación regenerable

## Contexto

`dbbancos` existe vacía. Aplicación será monolito .NET 10 + Angular, con MSSQL, Hangfire y datos financieros sensibles. Importaciones pueden modificar meses previos; cierres son regenerables manualmente.

## Objetivo

Crear fundación backend con configuración .NET estándar, schema migrable y features para importaciones, cuentas/auxiliares, catálogo y jobs de regeneración.

## Alcance permitido

* Solución .NET 10 Minimal API por features y configuración segura.
* appsettings.json sin secretos y appsettings.Development.json ignorado.
* EF Core/MSSQL, migración inicial y modelo auditado.
* Hangfire SQL Server + Hangfire.Console; jobs reciben IDs pequeños.
* Upload temporal, registro de importación, banderas de períodos pendientes y job de regeneración manual.
* Entidades para IBAN, propietario, auxiliares, moneda, tipo de cambio, asiento, conciliación N:N, auditoría y cierres FX.

## Fuera de alcance

* Aplicar migración contra dbbancos sin validación explícita del usuario.
* Lectores finales de todas las plantillas, IA Azure, dashboard Angular o autenticación.
* Eliminar registros automáticamente.

## Criterios de aceptación

* [ ] API compila, usa appsettings y no expone secretos.
* [ ] Migración genera schema sin datos financieros y puede revertirse en ambiente vacío.
* [ ] Endpoint de upload crea importación y job sin pasar bytes a Hangfire.
* [ ] Job de regeneración se encola solo por acción manual, muestra etapas en Console y marca reportes desactualizados hasta terminar.
* [ ] Todas las mutaciones generan auditoría; conciliación admite N:N.

## Riesgos

Riesgo alto: requiere aprobación explícita antes de implementar.

## Archivos afectados / probables

* `pendiente de confirmar`

## Plan técnico

1. Crear solución y estructura por features.
2. Crear configuración Development ignorada desde secreto local.
3. Modelar entidades, constraints e índices; generar migración sin aplicarla.
4. Configurar Hangfire y endpoints de salud, importación y regeneración.
5. Agregar pruebas de modelo, idempotencia y job.

## Pasos

1. Crear solución y estructura por features.
2. Crear configuración Development ignorada desde secreto local.
3. Modelar entidades, constraints e índices; generar migración sin aplicarla.
4. Configurar Hangfire y endpoints de salud, importación y regeneración.
5. Agregar pruebas de modelo, idempotencia y job.

## Salida esperada

Una solución backend .NET 10 compilable, con configuración segura, migración inicial no aplicada, endpoints de importación y regeneración, y pruebas de modelo, idempotencia y jobs.

## Validación

* [ ] dotnet build, dotnet test y revisión de migración.
* [ ] No secretos ni movimientos en archivos versionados.
* [ ] Aplicación de migración solo tras aprobación separada.

## Rollback

Eliminar solución creada antes de migrar. Si una migración se aplicara por autorización posterior, usar migración descendente o una base vacía de respaldo confirmado.

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

* Migrada al contrato canónico el 2026-07-18 12:42 CR.

## Issues vinculados

* ninguno
