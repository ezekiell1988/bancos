// Carga de credenciales SQL Server desde .local-secrets/{secretsFile}. Compartido por los
// servidores MCP que hablan con SQL Server (db-query, llm-audit-db). Los valores de
// Server/Database/User/Password nunca se registran (log) ni se devuelven en el resultado de
// un tool — solo se usan para construir la configuración de conexión que consume el driver mssql.

import fs from 'node:fs/promises';
import path from 'node:path';
import { ToolError } from './results.mjs';

export async function loadSqlServerConfig(projectRoot, secretsFile) {
  const secretsPath = path.join(projectRoot, '.local-secrets', secretsFile);
  let raw;
  try {
    raw = JSON.parse(await fs.readFile(secretsPath, 'utf8'));
  } catch {
    throw new ToolError(`falta .local-secrets/${secretsFile} o no contiene JSON válido`);
  }

  for (const key of ['Server', 'Database', 'User', 'Password']) {
    if (!String(raw[key] ?? '').trim()) throw new ToolError(`.local-secrets/${secretsFile}: falta el campo ${key}`);
  }

  const { host, port, instanceName } = parseServer(raw.Server);
  return {
    server: host,
    port,
    database: raw.Database,
    user: raw.User,
    password: raw.Password,
    options: { encrypt: true, trustServerCertificate: true, ...(instanceName ? { instanceName } : {}) },
    pool: { max: 4, min: 0, idleTimeoutMillis: 10000 },
    connectionTimeout: 15000,
  };
}

function parseServer(value) {
  const server = String(value ?? '').trim();
  const comma = server.match(/^(.+?),(\d+)$/);
  if (comma) return { host: comma[1], port: Number(comma[2]) };
  const instance = server.match(/^(.+?)\\(.+)$/);
  if (instance) return { host: instance[1], instanceName: instance[2] };
  return { host: server, port: 1433 };
}
