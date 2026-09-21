// Puerto de SqlQueryExecutor.cs: ejecuta una sentencia T-SQL como request independiente contra
// el pool compartido y normaliza el resultado a { status, rowsAffected, resultsets, error }.
// Un fallo de conexión o de sintaxis no lanza: se documenta como status "error" en el reporte,
// igual que la versión .NET, para no detener las consultas siguientes del lote.
import { safeSqlMessage, sanitizeRow } from '../../_shared/mssql-pool.mjs';
import { getProjectRoot } from './common.mjs';
import { getPool } from './database.mjs';

export async function executeQuery(query, timeoutSeconds) {
  try {
    const pool = await getPool(getProjectRoot());
    const request = pool.request();
    request.timeout = timeoutSeconds * 1000;
    const result = await request.query(query);

    const resultsets = [];
    for (const recordset of result.recordsets ?? []) {
      const columns = recordset.columns ? Object.keys(recordset.columns) : [];
      if (columns.length === 0) continue; // sin columnas = DDL/DML sin resultset, igual que FieldCount==0 en .NET
      resultsets.push({ columns, rows: recordset.map((row) => sanitizeRow(row)) });
    }

    return { status: 'ok', rowsAffected: result.rowsAffected ?? [], resultsets };
  } catch (err) {
    return { status: 'error', rowsAffected: [], resultsets: [], error: safeSqlMessage(err) };
  }
}
