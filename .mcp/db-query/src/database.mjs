import fs from "node:fs/promises";
import path from "node:path";
import crypto from "node:crypto";
import sql from "mssql";
import { ToolError, clamp, requiredString } from "./common.mjs";

const projectRoot = resolveProjectRoot(process.argv.slice(2));
const secretsPath = path.join(projectRoot, ".local-secrets", "db.json");
let poolPromise;

export async function databaseStatus({ checkConnection = false } = {}) {
  await readSecretConfig();
  const result = { configured: true, driver: "mssql", secretSource: ".local-secrets/db.json", connectionChecked: false };
  if (checkConnection) {
    const pool = await getPool();
    await pool.request().query("SELECT 1 AS ok");
    result.connectionChecked = true;
    result.connected = true;
  }
  return result;
}

export async function listTables() {
  const result = await (await getPool()).request().query("SELECT TABLE_SCHEMA AS [schema], TABLE_NAME AS [name], TABLE_TYPE AS [type] FROM INFORMATION_SCHEMA.TABLES ORDER BY TABLE_SCHEMA, TABLE_NAME");
  return saveReadResult("db_list_tables", normalizeResult(result, 500), {});
}

export async function describeTable(args) {
  const schema = identifier(args.schema ?? "dbo", "schema");
  const table = identifier(args.table, "table");
  const request = (await getPool()).request()
    .input("schema", sql.NVarChar(128), schema)
    .input("table", sql.NVarChar(128), table);
  const result = await request.query("SELECT COLUMN_NAME AS [name], DATA_TYPE AS [type], IS_NULLABLE AS [nullable], CHARACTER_MAXIMUM_LENGTH AS [maxLength], ORDINAL_POSITION AS [ordinal] FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA=@schema AND TABLE_NAME=@table ORDER BY ORDINAL_POSITION");
  if (result.recordset.length === 0) throw new ToolError("tabla no encontrada");
  return saveReadResult("db_describe_table", normalizeResult(result, 500), { schema, table });
}

export async function queryReadonly(args) {
  const query = validateReadonlyQuery(args.sql);
  const maxRows = clamp(args.maxRows, 1, 500, 100);
  const timeoutSeconds = clamp(args.timeoutSeconds, 1, 30, 10);
  return saveReadResult("db_query", await runQuery(query, { maxRows, timeoutSeconds }), { maxRows, timeoutSeconds });
}

async function runQuery(query, { maxRows, timeoutSeconds }) {
  const request = (await getPool()).request();
  request.requestTimeout = timeoutSeconds * 1000;
  const result = await request.query(`BEGIN TRY\n SET ROWCOUNT ${maxRows};\n ${query}\n SET ROWCOUNT 0;\nEND TRY\nBEGIN CATCH\n SET ROWCOUNT 0;\n THROW;\nEND CATCH`);
  return normalizeResult(result, maxRows);
}

function normalizeResult(result, maxRows) {
  const rows = result.recordset ?? [];
  assertSafeColumns(Object.keys(rows[0] ?? {}));
  return { total: rows.length, truncated: rows.length >= maxRows, rows: rows.map(sanitizeRow) };
}

async function saveReadResult(tool, result, metadata) {
  return { total: result.total, truncated: result.truncated, rows: result.rows, resultPath: await writeMarkdown(tool, result.rows, metadata) };
}

async function writeMarkdown(tool, rows, metadata) {
  const dir = path.join(projectRoot, ".local-output", "db-query");
  await fs.mkdir(dir, { recursive: true, mode: 0o700 });
  const filename = `${new Date().toISOString().replaceAll(":", "-").replaceAll(".", "-")}-${crypto.randomUUID().slice(0, 8)}-${tool}.md`;
  const details = Object.entries(metadata).map(([key, value]) => `- ${key}: ${String(value)}`).join("\n");
  const output = path.join(dir, filename);
  await fs.writeFile(output, `# ${tool}\n\nGenerado: ${new Date().toISOString()}\n\n${details}\n\n## Resultados (${rows.length})\n\n${markdownTable(rows)}\n`, { mode: 0o600 });
  return output;
}

function markdownTable(rows) {
  if (!rows.length) return "_Sin resultados._";
  const fields = [...new Set(rows.flatMap((row) => Object.keys(row)))];
  const cell = (value) => String(value ?? "").replaceAll("|", "\\|").replaceAll("\n", " ");
  return `| ${fields.join(" | ")} |\n| ${fields.map(() => "---").join(" | ")} |\n${rows.map((row) => `| ${fields.map((field) => cell(row[field])).join(" | ")} |`).join("\n")}`;
}

async function getPool() {
  poolPromise ??= sql.connect(await buildConfig());
  try { return await poolPromise; } catch (error) { poolPromise = undefined; throw new ToolError(`no fue posible conectar a SQL Server: ${safeSqlMessage(error)}`); }
}

async function buildConfig() {
  const value = await readSecretConfig();
  const { host, port, instanceName } = parseServer(value.Server);
  return { server: host, port, database: value.Database, user: value.User, password: value.Password, options: { encrypt: true, trustServerCertificate: true, ...(instanceName ? { instanceName } : {}) }, pool: { max: 4, min: 0, idleTimeoutMillis: 10000 }, requestTimeout: 10000, connectionTimeout: 10000 };
}

async function readSecretConfig() {
  let value;
  try { value = JSON.parse(await fs.readFile(secretsPath, "utf8")); } catch { throw new ToolError("falta .local-secrets/db.json o no contiene JSON válido"); }
  for (const key of ["Server", "Database", "User", "Password"]) requiredString(value[key], key);
  return value;
}

function validateReadonlyQuery(value) {
  const query = requiredString(value, "sql");
  if (!/^(select|with)\b/i.test(query)) throw new ToolError("solo se permiten SELECT o CTE de lectura");
  if (/[;]/.test(query) || /--|\/\*|\*\//.test(query)) throw new ToolError("no se permiten múltiples sentencias ni comentarios SQL");
  const forbidden = /\b(insert|update|delete|merge|drop|alter|create|truncate|grant|revoke|deny|exec|execute|backup|restore|dbcc|use|into|openrowset|openquery|opendatasource|bulk)\b/i;
  if (forbidden.test(query) || /\b(xp_|sp_)[a-z0-9_]+/i.test(query)) throw new ToolError("la consulta contiene una operación no permitida");
  return query;
}

function assertSafeColumns(columns) {
  const blocked = columns.find((name) => /password|passwd|secret|api.?key|credential|connection.?string|^(?!total|active|ai)token(?:value|hash)?$/i.test(name));
  if (blocked) throw new ToolError(`la consulta intenta devolver una columna sensible: ${blocked}`);
}

function sanitizeRow(row) { return Object.fromEntries(Object.entries(row).map(([key, value]) => [key, sanitizeValue(value)])); }
function sanitizeValue(value) {
  if (Buffer.isBuffer(value)) return `<binario ${value.length} bytes>`;
  if (value instanceof Date) return value.toISOString();
  if (typeof value === "string") {
    const redacted = value.replace(/(api[_ -]?key|token|password|secret)\s*[:=]\s*[^\s,;]+/gi, "$1=[REDACTED]");
    return redacted.length > 2000 ? `${redacted.slice(0, 2000)}…` : redacted;
  }
  return value;
}
function identifier(value, name) { const result = requiredString(value, name); if (!/^[A-Za-z_][A-Za-z0-9_$#@]*$/.test(result)) throw new ToolError(`${name} inválido`); return result; }
function parseServer(value) { const server = requiredString(value, "Server"); const comma = server.match(/^(.+?),(\d+)$/); if (comma) return { host: comma[1], port: Number(comma[2]) }; const instance = server.match(/^(.+?)\\(.+)$/); if (instance) return { host: instance[1], instanceName: instance[2] }; return { host: server, port: 1433 }; }
function safeSqlMessage(error) { const code = error?.code ?? error?.originalError?.info?.number; return code ? `código ${code}` : "verifica red y credenciales locales"; }
export async function resetSchemas(args) {
  if (args.confirm !== true) throw new ToolError("confirm debe ser true para ejecutar esta operación destructiva");
  const pool = await getPool();

  // 1. Listar y eliminar FKs de ambos schemas una por una
  for (const schema of ["HangFire", "dbo"]) {
    const fks = await pool.request().query(
      `SELECT OBJECT_SCHEMA_NAME(parent_object_id) AS s, OBJECT_NAME(parent_object_id) AS t, name AS fk
       FROM sys.foreign_keys WHERE schema_id = SCHEMA_ID('${schema}')`);
    for (const row of fks.recordset) {
      await pool.request().batch(`ALTER TABLE [${row.s}].[${row.t}] DROP CONSTRAINT [${row.fk}]`);
    }
  }

  // 2. Listar y eliminar tablas una por una
  for (const schema of ["HangFire", "dbo"]) {
    const tables = await pool.request().query(
      `SELECT TABLE_NAME AS name FROM INFORMATION_SCHEMA.TABLES
       WHERE TABLE_SCHEMA = '${schema}' AND TABLE_TYPE = 'BASE TABLE'`);
    for (const row of tables.recordset) {
      await pool.request().batch(`DROP TABLE [${schema}].[${row.name}]`);
    }
  }

  // 3. Eliminar schema HangFire
  const schemaExists = await pool.request().query("SELECT 1 AS ok FROM sys.schemas WHERE name = 'HangFire'");
  if (schemaExists.recordset.length > 0) {
    await pool.request().batch("DROP SCHEMA [HangFire]");
  }

  // 4. Verificar que quedó limpio
  const remaining = await pool.request().query(
    "SELECT TABLE_SCHEMA AS [schema], TABLE_NAME AS [name] FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' ORDER BY TABLE_SCHEMA, TABLE_NAME");
  return { dropped: true, remainingTables: remaining.recordset };
}

function resolveProjectRoot(argv) { const index = argv.findIndex((arg) => arg === "--project-root"); const direct = index >= 0 ? argv[index + 1] : argv.find((arg) => arg.startsWith("--project-root="))?.slice(15); return path.resolve(direct ?? process.env.DB_MCP_PROJECT_ROOT ?? process.cwd()); }
