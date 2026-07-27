import fs from "node:fs/promises";
import path from "node:path";
import { ToolError, clamp, requiredString } from "./common.mjs";

const writePattern = /\b(insert|update|delete|merge|drop|alter|create|truncate|grant|revoke|deny|backup|restore|dbcc|exec|execute|openrowset|openquery|opendatasource|bulk)\b/gi;

export function createDatabase({ profile, projectRoot, sql }) {
  const secretsPath = path.join(projectRoot, ".local-secrets", profile.secretsFile);
  let poolPromise;

  return { inspectScript, execScript, loadActiveSponsorshipSubscriptions };

  function inspectScript(script) {
    const detectedOperations = [...new Set([...String(script).matchAll(writePattern)].map((match) => match[1].toUpperCase()))];
    if (/\b(xp_|sp_)[a-z0-9_]+/i.test(script)) detectedOperations.push("PROCEDIMIENTO_SISTEMA");
    return { detectedOperations: [...new Set(detectedOperations)], requiresApply: detectedOperations.length > 0 };
  }

  async function execScript({ sql: scriptValue, timeoutSeconds, apply }) {
    const script = requiredString(scriptValue, "sql");
    const preview = inspectScript(script);
    if (profile.requireApply && preview.requiresApply && apply !== true) return { applied: false, ...preview };
    const request = (await getPool()).request();
    request.requestTimeout = clamp(timeoutSeconds, 1, profile.maxTimeoutSeconds, profile.defaultTimeoutSeconds) * 1000;
    let result;
    try {
      result = await request.query(script);
    } catch (error) {
      throw new ToolError(safeSqlMessage(error));
    }
    const recordsets = result.recordsets ?? (result.recordset ? [result.recordset] : [[]]);
    const resultsets = recordsets.map((rows, index) => {
      const columns = Object.keys(rows[0] ?? {});
      assertSafeColumns(columns);
      return { index, total: rows.length, rows: rows.map(sanitizeRow) };
    });
    return { applied: true, ...preview, rowsAffected: result.rowsAffected ?? [], resultsets, count: resultsets.length };
  }

  async function loadActiveSponsorshipSubscriptions() {
    const request = (await getPool()).request();
    request.requestTimeout = profile.defaultTimeoutSeconds * 1000;
    try {
      const result = await request.query(`
        SELECT SponsorshipSubscriptionId, SubscriptionId, Name, OfferId,
               Currency, Locale, RegionInfo, TenantId, ClientId, ClientSecret
        FROM [amgt].[SponsorshipSubscription]
        WHERE IsActive = 1
        ORDER BY SortOrder, SponsorshipSubscriptionId;
      `);
      return result.recordset ?? [];
    } catch (error) {
      throw new ToolError(safeSqlMessage(error));
    }
  }

  async function getPool() {
    poolPromise ??= sql.connect(await buildConfig());
    try {
      return await poolPromise;
    } catch (error) {
      poolPromise = undefined;
      throw new ToolError(`no fue posible conectar a SQL Server: ${safeSqlMessage(error)}`);
    }
  }

  async function buildConfig() {
    let value;
    try {
      value = JSON.parse(await fs.readFile(secretsPath, "utf8"));
    } catch {
      throw new ToolError(`falta .local-secrets/${profile.secretsFile} o no contiene JSON válido`);
    }
    for (const key of ["Server", "Database", "User", "Password"]) requiredString(value[key], key);
    const { host, port, instanceName } = parseServer(value.Server);
    return { server: host, port, database: value.Database, user: value.User, password: value.Password, options: { encrypt: true, trustServerCertificate: true, ...(instanceName ? { instanceName } : {}) }, pool: { max: 4, min: 0, idleTimeoutMillis: 10000 }, requestTimeout: 10000, connectionTimeout: 10000 };
  }
}

function assertSafeColumns(columns) {
  const blocked = columns.find((name) => /(password|passwd|secret|token|api.?key|credential|connection.?string)/i.test(name));
  if (blocked) throw new ToolError(`la consulta intenta devolver una columna sensible: ${blocked}`);
}

function sanitizeRow(row) {
  return Object.fromEntries(Object.entries(row).map(([key, value]) => [key, sanitizeValue(value)]));
}

function sanitizeValue(value) {
  if (Buffer.isBuffer(value)) return `<binario ${value.length} bytes>`;
  if (value instanceof Date) return value.toISOString();
  if (typeof value === "string" && value.length > 2000) return `${value.slice(0, 2000)}...`;
  return value;
}

function parseServer(value) {
  const server = requiredString(value, "Server");
  const comma = server.match(/^(.+?),(\d+)$/);
  if (comma) return { host: comma[1], port: Number(comma[2]) };
  const instance = server.match(/^(.+?)\\(.+)$/);
  if (instance) return { host: instance[1], instanceName: instance[2] };
  return { host: server, port: 1433 };
}

function safeSqlMessage(error) {
  const code = error?.code ?? error?.originalError?.info?.number;
  const message = redactSqlErrorMessage(error?.originalError?.info?.message ?? error?.originalError?.message ?? error?.message);
  if (code && message) return `código ${code}: ${message}`;
  return code ? `código ${code}` : "verifica red y credenciales locales";
}

function redactSqlErrorMessage(value) {
  const message = String(value ?? "").replace(/\s+/g, " ").trim();
  if (!message) return "";

  return message
    .replace(/\b(password|pwd|token|secret|api[-_ ]?key|credential|connection\s*string)\s*[:=]\s*(?:"[^"]*"|'[^']*'|[^;\s,]+)/gi, "$1=[redacted]")
    .replace(/\b(server|data\s*source|user\s*id|uid|initial\s*catalog|database)\s*=\s*[^;]+;?/gi, "[connection setting redacted]")
    .slice(0, 500);
}