// Puerto de DbQueryPaths.ResolveReportPath: resuelve dónde escribir el Markdown de db_exec.
// nombre suelto -> .mcp/db-query/queries/; ruta relativa con directorio -> raíz del proyecto;
// ruta absoluta -> se usa tal cual. El directorio destino se crea si no existe.
import fs from 'node:fs';
import path from 'node:path';
import { ToolError } from '../../_shared/results.mjs';

export function resolveReportPath(projectRoot, mcpRoot, filePath) {
  const value = String(filePath ?? '').trim();
  if (!value) throw new ToolError('filePath requerido');
  if (!value.toLowerCase().endsWith('.md')) throw new ToolError('filePath debe tener extensión .md');

  const hasDirectory = value.includes('/') || value.includes('\\');
  const result = path.isAbsolute(value)
    ? path.resolve(value)
    : hasDirectory
      ? path.resolve(projectRoot, value)
      : path.resolve(mcpRoot, 'queries', value);

  fs.mkdirSync(path.dirname(result), { recursive: true });
  return result;
}

/** Convierte la ruta absoluta escrita a la forma de respuesta: relativa (forward slashes) si cae
 * dentro del proyecto, absoluta tal cual si queda fuera — igual que DbQueryPaths en .NET. */
export function toResponsePath(projectRoot, absolutePath) {
  const relative = path.relative(projectRoot, absolutePath);
  if (relative.startsWith('..')) return absolutePath;
  return relative.split(path.sep).join('/');
}
