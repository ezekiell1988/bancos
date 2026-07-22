---
name: llm-audit-db
description: "Persistencia de LLM audit logs en SQL Server via tbLlmAuditLog. Usar cuando se quiera activar la persistencia de audit logs en BD para debug en producción, consultar entradas de Art Library, o diagnosticar errores de Azure AI Foundry en producción. Triggers: 'ver logs de IA en producción', 'activar audit en BD', 'consultar tbLlmAuditLog', 'debug de generación en producción'."
---

# LLM Audit DB — Persistencia en SQL Server (Marketing1on1)

Permite persisitir entradas de `ILlmAuditService` en la base de datos SQL Server del proyecto, para diagnóstico en producción donde el filesystem es efímero (Azure Container Apps).

---

## ¿Cuándo usarlo?

Cuando se reporta un error de generación de imagen en producción y los logs de archivo no están disponibles. Los logs en BD sobreviven reinicios del contenedor.

---

## Estructura de tabla — `tbLlmAuditLog`

Sigue las convenciones de nomenclatura del proyecto (ver `ia/README.md`):

```sql
CREATE TABLE dbo.tbLlmAuditLog (
    idLlmAuditLog  int          IDENTITY(1,1) NOT NULL
        CONSTRAINT PK_tbLlmAuditLog PRIMARY KEY,
    idClient       int          NULL,          -- FK tbClient (nullable: logs sin cliente)
    sessionId      nvarchar(50) NULL,          -- correlación frontend↔backend
    level          nvarchar(20) NOT NULL,      -- STARTUP | EVENT | DECISION | ERROR | BROWSER
    category       nvarchar(100) NOT NULL,     -- componente: ArtLibrary, INTEGRATION...
    intent         nvarchar(200) NOT NULL,     -- descripción de la acción
    result         nvarchar(500) NOT NULL,     -- resultado / estado
    context        nvarchar(MAX) NULL,         -- JSON adicional (preview de body, etc.)
    source         nvarchar(20) NOT NULL       -- 'server' | 'browser'
        CONSTRAINT DF_tbLlmAuditLog_source DEFAULT 'server',
    createAt       datetime2    NOT NULL
        CONSTRAINT DF_tbLlmAuditLog_createAt DEFAULT GETUTCDATE()
);

-- Índices para queries de diagnóstico
CREATE INDEX IX_tbLlmAuditLog_idClient  ON dbo.tbLlmAuditLog (idClient);
CREATE INDEX IX_tbLlmAuditLog_createAt  ON dbo.tbLlmAuditLog (createAt DESC);
CREATE INDEX IX_tbLlmAuditLog_level     ON dbo.tbLlmAuditLog (level);
```

### Convenciones aplicadas (`ia/README.md`)

| Regla | Valor |
|-------|-------|
| Prefijo tabla | `tb` → `tbLlmAuditLog` |
| PK | `id` + nombre sin `tb` en camelCase → `idLlmAuditLog` |
| FK | `id` + entidad referenciada → `idClient` |
| Timestamp | `createAt` (sin `d` final) |
| Campos descriptivos | camelCase sin sufijo de entidad |

---

## Estado de implementación

| Componente | Estado |
|-----------|--------|
| `Models/LlmAuditLog.cs` | ✅ implementado |
| `Data/Configurations/LlmAuditLogConfiguration.cs` | ✅ implementado |
| `DbSet<LlmAuditLog> LlmAuditLogs` en `AppDbContext` | ✅ implementado |
| `PersistToDb` flag en `LlmAuditOptions` | ✅ implementado |
| Lógica fire-and-forget en `LlmAuditService` | ✅ implementado |
| Tabla `tbLlmAuditLog` en BD | ✅ creada por script |
| Migración EF Core | ⚠️ pendiente (tabla ya existe — usar `IF NOT EXISTS`) |

> La persistencia en BD ya es funcional. Solo falta crear la migración EF para que sea
> reproducible en otros entornos. Activar con `PersistToDb=true` en appsettings.

---

## Configuración para activar

**appsettings.json** (base — desactivado):
```json
"LlmAudit": {
  "Enabled": false,
  "PersistToDb": false,
  "LogPath": "logs/llm-audit.md",
  "MaxFileSizeKb": 2048
}
```

**Para activar en dev** — agregar a `appsettings.Development.json`:
```json
"LlmAudit": {
  "Enabled": true,
  "PersistToDb": true,
  "LogPath": "logs/llm-audit.md"
}
```

**Para activar en producción** — variable de entorno en Azure Container Apps:
```bash
az containerapp update \
  --name marketing1on1-api \
  --resource-group rg-clickeat \
  --set-env-vars \
    "LlmAudit__Enabled=true" \
    "LlmAudit__PersistToDb=true"
```

---

## ⚠️ Ordenar por `idLlmAuditLog`, NO por `createAt`

Las inserciones son fire-and-forget async; `createAt` puede llegar tarde al orden físico.
`idLlmAuditLog` es IDENTITY (FIFO) y representa el orden real de los eventos.

---

## Queries de diagnóstico

### Últimas 50 entradas (debug general)

```sql
SELECT TOP 50
    idLlmAuditLog,
    idClient,
    level,
    category,
    intent,
    result,
    LEFT(context, 300) AS context,
    FORMAT(createAt AT TIME ZONE 'UTC' AT TIME ZONE 'Central America Standard Time', 'HH:mm:ss') AS hora
FROM dbo.tbLlmAuditLog
ORDER BY idLlmAuditLog DESC;
```

### Solo errores recientes

```sql
SELECT TOP 30
    idLlmAuditLog,
    idClient,
    category,
    intent,
    result,
    context,
    FORMAT(createAt AT TIME ZONE 'UTC' AT TIME ZONE 'Central America Standard Time', 'HH:mm:ss') AS hora
FROM dbo.tbLlmAuditLog
WHERE level = 'ERROR'
ORDER BY idLlmAuditLog DESC;
```

### Flujo completo de generación por cliente

```sql
SELECT
    idLlmAuditLog,
    level,
    category,
    intent,
    result,
    LEFT(context, 200) AS context,
    FORMAT(createAt AT TIME ZONE 'UTC' AT TIME ZONE 'Central America Standard Time', 'HH:mm:ss') AS hora
FROM dbo.tbLlmAuditLog
WHERE idClient = 1   -- reemplazar con el idClient real
  AND category IN ('ArtLibrary', 'INTEGRATION')
ORDER BY idLlmAuditLog;
```

### Consultar con dbQuery MCP

Usar `db_logs` para auditoría reciente o `db_query` con SELECT/CTE. El resultado queda en `.local-output/db-query/`.

---

## Niveles de log y sus fuentes

| Nivel | Origen | Ejemplo |
|-------|--------|---------|
| `STARTUP` | `LogStartup()` | Configuración Azure AI cargada |
| `EVENT` | `LogEvent()` | `GenerateArtwork iniciado`, respuesta Azure |
| `DECISION` | `LogDecision()` | `Usar edits API por imagen de referencia` |
| `ERROR` | `LogError()` | Excepción en llamada Azure, error 400 |
| `BROWSER` | `LogClientEvent()` | Error de generación reportado desde frontend |
