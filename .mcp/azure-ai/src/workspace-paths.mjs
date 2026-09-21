// Puerto de AzurePaths.Resolve: resuelve rutas del workspace rechazando traversal.
import { existsSync } from 'node:fs';
import path from 'node:path';
import { ToolError } from '../../_shared/results.mjs';

export function resolveWorkspacePath(projectRoot, targetPath, mustExist = false) {
  if (!targetPath) throw new ToolError('targetPath debe ser un string no vacío');

  const root = path.resolve(projectRoot);
  const resolved = path.isAbsolute(targetPath) ? path.resolve(targetPath) : path.resolve(root, targetPath);

  const relative = path.relative(root, resolved);
  if (relative.startsWith('..') || path.isAbsolute(relative)) {
    throw new ToolError(`Acceso denegado fuera del workspace: "${targetPath}"`);
  }

  if (mustExist && !existsSync(resolved)) {
    throw new ToolError(`El archivo o directorio no existe: "${targetPath}" (${resolved})`);
  }

  return resolved;
}
