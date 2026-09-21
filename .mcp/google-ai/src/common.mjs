// Adaptador delgado de configuración de dominio: solo --project-root.
import { resolveProjectRoot } from '../../_shared/cli.mjs';

let projectRoot = null;

export function initProjectRoot(argv = process.argv) {
  projectRoot = resolveProjectRoot(argv);
  return projectRoot;
}

export function getProjectRoot() {
  if (!projectRoot) throw new Error('initProjectRoot() no se ha llamado todavía');
  return projectRoot;
}

export { ToolError, clamp } from '../../_shared/results.mjs';
