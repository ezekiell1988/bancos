# Estrategia de entornos de base de datos — Bancos

## Dos entornos, dos estrategias

### Dev (local)

- **Contenedor**: SQL Server en Docker (puerto local)
- **Secretos**: `.local-secrets/db.json`
- **Estrategia**: drop & recreate libremente
- **Migraciones**: se aplican siempre desde cero; NO se acumulan migraciones innecesarias
- **Datos**: desechables, de prueba; pueden borrarse en cualquier momento
- **Cuándo resetear**: antes de probar un cambio de esquema, cuando se necesita estado limpio, cuando hay deuda de migraciones pendientes
- **Herramienta**: `01_dev_reset.ps1`

### Prod (Azure SQL — pendiente de provisionar)

- **Contenedor**: Azure SQL (contenedor/servidor aún por definir)
- **Secretos**: `.local-secrets/dbProd.json`
- **Estrategia**: solo migraciones EF incrementales, jamás drop
- **Datos**: producción real de la familia Baltodano-Soto; históricos intocables
- **Cuándo migrar**: al desplegar una versión con nuevas migraciones EF
- **Herramienta**: `02_prod_migrate.ps1 -Confirm`

## Flujo típico de trabajo

```
[Trabajo en dev]
1. Hacer cambios de esquema en modelos/DbContext
2. NO crear migraciones para cambios intermedios en dev
3. Usar 01_dev_reset.ps1 para resetear y probar con esquema limpio
4. Cuando el esquema está estable → crear una sola migración EF

[Despliegue a prod]
5. dotnet ef migrations add <NombreSignificativo>
6. Revisar el SQL generado manualmente
7. 02_prod_migrate.ps1 -WhatIf   → verificar qué se aplicará
8. 02_prod_migrate.ps1 -Confirm  → aplicar a prod

[Pruebas con datos reales]
9. 04_diff_envs.py --detail       → ver diferencias
10. 03_prod_to_dev.py             → traer datos de prod a dev
11. Probar en dev con datos reales
```

## Reglas de oro

| Regla | Dev | Prod |
|---|---|---|
| `dotnet ef database drop` | ✅ libre | ❌ nunca |
| `dotnet ef database update` | ✅ siempre que se quiera | ✅ solo con revisión |
| `dotnet ef migrations add` | Solo cuando esquema esté listo | N/A (se genera en dev) |
| Scripts Python de sync | ✅ destino de escritura | ❌ solo fuente de lectura |
| Exponer datos en logs/IA | ❌ nunca (ni dev) | ❌ nunca |

## Connection strings

Ambos archivos de secretos tienen el mismo esquema que `db.example.json`:

```json
{
  "Server": "host,puerto",
  "Database": "nombre",
  "User": "usuario-sql",
  "Password": "contraseña",
  "Options": "-No"
}
```

El `LocalDatabaseConfiguration.RequireConnectionString()` en `Infrastructure/LocalDatabaseConfiguration.cs`
lee `.local-secrets/db.json` en design-time. Para prod, los scripts leen `dbProd.json` directamente
y pasan el connection string via `--connection` al CLI de EF.

## ADRs relacionados

- `DEV-ENV-local-sqlserver.md` — entorno de desarrollo local con Docker y secretos locales
- `ADR-01` — monolito .NET + Angular + MSSQL
