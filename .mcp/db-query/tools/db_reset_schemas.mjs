const RESET_SQL = `
DECLARE @sql NVARCHAR(MAX) = N'';

SELECT @sql += 'ALTER TABLE ' + QUOTENAME(s.name) + '.' + QUOTENAME(t.name) + ' NOCHECK CONSTRAINT ALL;' + CHAR(10)
FROM sys.tables t JOIN sys.schemas s ON s.schema_id = t.schema_id
WHERE s.name IN ('dbo', 'HangFire');
EXEC sp_executesql @sql;

SET @sql = N'';
SELECT @sql += 'ALTER TABLE ' + QUOTENAME(s.name) + '.' + QUOTENAME(t.name) + ' DROP CONSTRAINT ' + QUOTENAME(fk.name) + ';' + CHAR(10)
FROM sys.foreign_keys fk
JOIN sys.tables t ON t.object_id = fk.parent_object_id
JOIN sys.schemas s ON s.schema_id = t.schema_id
WHERE s.name IN ('dbo', 'HangFire');
EXEC sp_executesql @sql;

SET @sql = N'';
SELECT @sql += 'DROP TABLE ' + QUOTENAME(s.name) + '.' + QUOTENAME(t.name) + ';' + CHAR(10)
FROM sys.tables t JOIN sys.schemas s ON s.schema_id = t.schema_id
WHERE s.name IN ('dbo', 'HangFire');
EXEC sp_executesql @sql;

IF EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'HangFire')
  DROP SCHEMA HangFire;
`.trim();

export const definition = {
  name: "db_reset_schemas",
  order: 90,
  description: "Elimina TODAS las tablas del schema dbo y elimina completamente el schema HangFire (tablas + schema). Requiere confirmación explícita con confirm: true. Usar solo para regenerar migraciones de EF Core desde cero.",
  inputSchema: {
    type: "object",
    properties: {
      confirm: { type: "boolean", description: "Debe ser true para ejecutar la operación destructiva." },
    },
    required: ["confirm"],
    additionalProperties: false,
  },
};

export async function execute(runtime, args) {
  if (args?.confirm !== true) {
    return {
      applied: false,
      requiresApply: true,
      note: "Operación destructiva: elimina todas las tablas de dbo y el schema HangFire completo. Repite la llamada con confirm:true para ejecutarla.",
    };
  }

  const result = await runtime.database.execScript({
    sql: RESET_SQL,
    timeoutSeconds: runtime.profile.maxTimeoutSeconds,
    apply: true,
  });

  return { applied: true, operations: result.detectedOperations, rowsAffected: result.rowsAffected };
}
