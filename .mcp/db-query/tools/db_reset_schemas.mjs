// Herramienta específica de Bancos: reinicia dbo/HangFire para regenerar migraciones EF Core en dev.
import { ToolError, getProjectRoot } from '../src/common.mjs';
import { executeQuery } from '../src/executor.mjs';

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

export default {
  name: 'db_reset_schemas',

  description:
    'Elimina TODAS las tablas del schema dbo y el schema HangFire completo. Requiere confirm: true. ' +
    'Usar solo para regenerar migraciones de EF Core desde cero en dev.',

  inputSchema: {
    type: 'object',
    properties: {
      confirm: { type: 'boolean', description: 'Debe ser true para ejecutar la operación destructiva.' },
    },
    required: ['confirm'],
    additionalProperties: false,
  },

  async handler(args) {
    getProjectRoot();
    if (args?.confirm !== true) {
      return {
        applied: false,
        requiresApply: true,
        note: 'Operación destructiva: elimina todas las tablas de dbo y el schema HangFire. Repite la llamada con confirm:true.',
      };
    }
    const result = await executeQuery(RESET_SQL, 120);
    if (result.status !== 'ok') throw new ToolError(`db_reset_schemas falló: ${result.error ?? result.status}`);
    return { applied: true, rowsAffected: result.rowsAffected };
  },

  async smoke({ callTool, check, toolJson }) {
    const res = toolJson(await callTool('db_reset_schemas', { confirm: false }));
    check('db_reset_schemas sin confirm no ejecuta', res.applied === false, JSON.stringify(res));
  },
};
