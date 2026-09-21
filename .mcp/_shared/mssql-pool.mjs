// Utilidades de conexión mssql compartidas por db-query y llm-audit-db: pool reutilizable,
// sanitización de filas (binarios/fechas) y mensajes de error seguros (sin credenciales).
// Cada servidor conserva su propia lógica de ejecución (SQL arbitrario vs. consultas fijas
// de solo lectura) — solo la plomería de conexión/resultado vive aquí.

import sql from 'mssql';
import { ToolError } from './results.mjs';

/**
 * Crea un getPool(projectRoot) con pool cacheado propio (closure) a partir de un loader de
 * configuración. Cada servidor llama a createPoolGetter() una sola vez al cargar su
 * database.mjs, así que el pool no se comparte entre servidores ni entre instancias con
 * distinto --secrets-file corriendo en el mismo proceso.
 */
export function createPoolGetter(loadConfig) {
  let poolPromise;

  async function connect(projectRoot) {
    const config = await loadConfig(projectRoot);
    return sql.connect(config);
  }

  return async function getPool(projectRoot) {
    poolPromise ??= connect(projectRoot);
    try {
      return await poolPromise;
    } catch (err) {
      poolPromise = undefined;
      throw new ToolError(`no fue posible conectar a SQL Server: ${safeSqlMessage(err)}`);
    }
  };
}

export function sanitizeRow(row) {
  return Object.fromEntries(Object.entries(row).map(([key, value]) => [key, sanitizeValue(value)]));
}

export function sanitizeValue(value) {
  if (Buffer.isBuffer(value)) return `<binario ${value.length} bytes>`;
  if (value instanceof Date) return value.toISOString();
  return value;
}

export function safeSqlMessage(err) {
  const code = err?.code ?? err?.originalError?.info?.number;
  const message = redact(err?.originalError?.info?.message ?? err?.originalError?.message ?? err?.message);
  if (code && message) return `código ${code}: ${message}`;
  return message || (code ? `código ${code}` : 'error desconocido al ejecutar la consulta');
}

function redact(value) {
  const message = String(value ?? '').replace(/\s+/g, ' ').trim();
  if (!message) return '';
  return message
    .replace(/\b(password|pwd|token|secret|api[-_ ]?key|credential|connection\s*string)\s*[:=]\s*(?:"[^"]*"|'[^']*'|[^;\s,]+)/gi, '$1=[redacted]')
    .replace(/\b(server|data\s*source|user\s*id|uid|initial\s*catalog|database)\s*=\s*[^;]+;?/gi, '[connection setting redacted]')
    .slice(0, 500);
}
