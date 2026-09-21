// Helpers puros de parseo/validación de argv — sin estado.
// Cada servidor conserva su propio singleton de inicialización (src/common.mjs)
// y delega en estas funciones para no duplicar la lógica de resolución.

import path from 'node:path';
import { existsSync } from 'node:fs';

/** Lee un flag `--flag valor` de argv, o retorna fallback si no está presente. */
export function getArgvFlag(argv, flag, fallback = null) {
  const i = argv.indexOf(flag);
  return i !== -1 && argv[i + 1] ? argv[i + 1] : fallback;
}

/** Resuelve un flag de raíz de proyecto/carpeta contra cwd y valida que exista. */
export function resolveProjectRoot(argv = process.argv, flag = '--project-root') {
  const candidate = getArgvFlag(argv, flag, process.cwd());
  const resolved = path.resolve(candidate);
  if (!existsSync(resolved)) {
    throw new Error(`raíz de proyecto no encontrada: ${resolved} (usar ${flag})`);
  }
  return resolved;
}

/** Resuelve un nombre de archivo de secretos de argv — solo nombre, sin separadores de ruta. */
export function resolveSecretsFileName(argv, defaultName, flag = '--secrets-file') {
  const candidate = getArgvFlag(argv, flag, defaultName);
  if (candidate.includes('/') || candidate.includes('\\') || candidate.includes('..')) {
    throw new Error(`${flag} inválido: ${candidate} (solo nombre de archivo)`);
  }
  return candidate;
}
