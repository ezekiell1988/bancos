// Adaptador delgado de configuración de dominio: --project-root, --secrets-file y --server-name.
// El mismo server.mjs se registra dos veces (dbQuery/db_voicebot.json, dbQueryClickeat/db_dev_clickeat.json)
// solo cambiando estos flags — sin lógica distinta entre instancias.
import { getArgvFlag, resolveProjectRoot, resolveSecretsFileName } from '../../_shared/cli.mjs';

let projectRoot = null;
let secretsFile = null;
let serverName = null;

/** Resuelve y guarda --project-root/--secrets-file/--server-name desde argv. Llamado una vez al arrancar. */
export function initConfig(argv = process.argv) {
  projectRoot = resolveProjectRoot(argv);
  secretsFile = resolveSecretsFileName(argv, 'sqlserver.json');
  serverName = getArgvFlag(argv, '--server-name', 'dbQuery');
  return { projectRoot, secretsFile, serverName };
}

/** Raíz de proyecto ya resuelta por initConfig(). */
export function getProjectRoot() {
  if (!projectRoot) throw new Error('initConfig() no se ha llamado todavía');
  return projectRoot;
}

/** Nombre de archivo de secretos SQL ya resuelto por initConfig(). */
export function getSecretsFile() {
  if (!secretsFile) throw new Error('initConfig() no se ha llamado todavía');
  return secretsFile;
}

/** serverInfo.name ya resuelto por initConfig() (default: "dbQuery"). */
export function getServerName() {
  if (!serverName) throw new Error('initConfig() no se ha llamado todavía');
  return serverName;
}

export { ToolError, clamp } from '../../_shared/results.mjs';
