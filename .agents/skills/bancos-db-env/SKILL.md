---
name: bancos-db-env
description: >
  Gestión de dos entornos de base de datos en el proyecto Bancos: dev (local Docker SQL Server,
  recreable libremente) y prod (Azure SQL, solo migraciones EF sin pérdida de datos).
  Incluye ejemplos para: resetear dev, aplicar migraciones a prod, sincronizar registros
  prod→dev y comparar estado dev↔prod. Triggers: entorno base de datos, resetear BD,
  migrar prod, sincronizar prod dev, volcar prod a local, merge prod dev, db env, EF prod.
---

# bancos-db-env — Gestión de entornos de base de datos

## Contexto del sistema

El proyecto Bancos mantiene **dos entornos de BD** con estrategias radicalmente distintas:

| | Dev (local) | Prod |
|---|---|---|
| Secretos | `.local-secrets/db.json` | `.local-secrets/dbProd.json` |
| Contenedor | Docker SQL Server local | Azure SQL (aún no definido) |
| Estrategia | Drop + recrear libremente | Solo migraciones EF, datos históricos intocables |
| Migraciones | `dotnet ef database update` local | `dotnet ef database update` apuntando a prod |
| Datos | Desechables / de prueba | Producción real de la familia Baltodano-Soto |

## Estructura de secretos

Ambos archivos siguen el esquema de `.local-secrets/db.example.json`:

```json
{
  "Server": "host,puerto",
  "Database": "nombre-db",
  "User": "usuario-sql",
  "Password": "contraseña",
  "Options": "-No"
}
```

**NUNCA exponer el contenido de estos archivos en logs, prompts o salidas de IA.**  
Leer solo las claves necesarias para construir la connection string.

## Proyectos EF

- Proyecto EF: `src/Bancos.Api/Bancos.Api.csproj`
- DbContext: `BancosDbContext` (namespace `Bancos.Api.Data`)
- Migraciones: `src/Bancos.Api/Migrations/`
- El `BancosDbContextFactory` lee `.local-secrets/db.json` por defecto en design-time

## Flujos disponibles

### 1. Reset completo de dev (local)

Uso: cuando se quiere empezar desde cero en dev, agregar seed data nueva, o probar cambios de esquema sin crear migraciones.

Ver [examples/01_dev_reset.ps1](./examples/01_dev_reset.ps1)

**Pasos**:
1. Detener la API local
2. `dotnet ef database drop --force`
3. `dotnet ef database update`
4. Reiniciar API (Hangfire crea sus tablas al arrancar)

### 2. Migrar prod (sin pérdida de datos)

Uso: cuando se despliega una nueva versión con migraciones EF nuevas. Solo aplica schema changes; los datos históricos quedan intactos.

Ver [examples/02_prod_migrate.ps1](./examples/02_prod_migrate.ps1)

**Pasos**:
1. Leer connection string desde `dbProd.json`
2. `dotnet ef database update --connection "..."` apuntando a prod
3. Verificar con `dotnet ef migrations list`

### 3. Sincronizar prod → dev (merge de registros reales)

Uso: cuando se quiere probar en local con datos reales de prod (errores de parseo, registros específicos, etc.).

Ver [examples/03_prod_to_dev.py](./examples/03_prod_to_dev.py)

**Qué copia**: tablas de referencia (Owners, Currencies, Accounts, AccountAuxiliaries, Categories, ClassificationRules, ExchangeRates) y opcionalmente transacciones recientes. Usa `MERGE` o INSERT con ON CONFLICT para no duplicar.

### 4. Comparar dev ↔ prod (diagnóstico bidireccional)

Uso: antes de hacer sync o para entender qué hay en cada entorno.

Ver [examples/04_diff_envs.py](./examples/04_diff_envs.py)

**Qué reporta**: conteos por tabla en ambos entornos, filas en prod que no están en dev (por PK/fingerprint), filas en dev que no están en prod.

## Reglas críticas

- Prod: **solo** `dotnet ef database update` — nunca `database drop`
- Dev: libre para drop/recreate en cualquier momento
- Nunca usar `--force` en prod
- Los archivos `.local-secrets/*.json` no deben aparecer en salidas de IA
- Para SQL directo en dev, usar `mcp__dbQuery__db_query` (solo lectura)
- Todo script que toque prod debe tener confirmación explícita antes de ejecutar

## Referencias

Ver [references/env-strategy.md](./references/env-strategy.md) para la estrategia completa de entornos y decisiones ADR relacionadas.
