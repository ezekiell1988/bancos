// Puerto de SqlQueryExecutor.cs#BuildConnectionString: conexión SQL Server para este servidor.
// La plomería de pool/sanitización vive en .mcp/_shared/mssql-pool.mjs; aquí solo se cablea
// la carga de credenciales propia de db-query (.local-secrets/{secretsFile}).
import { createPoolGetter } from '../../_shared/mssql-pool.mjs';
import { loadSqlServerConfig } from '../../_shared/sql-secrets.mjs';
import { getSecretsFile } from './common.mjs';

export const getPool = createPoolGetter((projectRoot) => loadSqlServerConfig(projectRoot, getSecretsFile()));
