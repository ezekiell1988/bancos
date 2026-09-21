// Puerto de WorkflowPaths.cs: resuelve projectRoot/iaRoot desde argv, incluida la
// configuración local de VS Code Studio (--runtime studio --local-secrets-file ...).
import { readFileSync, existsSync } from 'node:fs';
import path from 'node:path';

function argFlag(args, flag) {
  const eq = `${flag}=`;
  for (let i = 0; i < args.length; i++) {
    if (args[i] === flag && i + 1 < args.length) return args[i + 1];
    if (args[i].startsWith(eq)) return args[i].slice(eq.length);
  }
  return null;
}

function requiredString(obj, name, configurationPath) {
  const value = obj?.[name];
  if (typeof value !== 'string' || value.trim() === '') {
    throw new Error(`La configuración local debe definir ${name}: ${configurationPath}`);
  }
  return value;
}

function requiredInitials(obj, configurationPath) {
  const value = requiredString(obj, 'initials', configurationPath);
  if (value.length < 1 || value.length > 3 || !/^[A-Za-z0-9]+$/.test(value)) {
    throw new Error(`initials debe tener entre 1 y 3 caracteres alfanuméricos: ${configurationPath}`);
  }
  return value.toUpperCase();
}

function fromStudioConfiguration(requestedProjectRoot, localSecretsFile) {
  const fallbackRoot = path.resolve(requestedProjectRoot ?? process.env.IA_MCP_PROJECT_ROOT ?? process.cwd());
  const configurationPath = path.resolve(
    localSecretsFile ?? process.env.IA_WORKFLOW_LOCAL_SECRETS_FILE ?? path.join(fallbackRoot, '.local-secrets', 'ia-workflow.json'),
  );
  let parsed;
  try {
    parsed = JSON.parse(readFileSync(configurationPath, 'utf8'));
  } catch (err) {
    throw new Error(`No se pudo cargar la configuración local requerida: ${configurationPath}`, { cause: err });
  }
  const name = requiredString(parsed, 'name', configurationPath);
  const initials = requiredInitials(parsed, configurationPath);
  const email = requiredString(parsed, 'email', configurationPath);
  const platform = requiredString(parsed, 'platform', configurationPath);
  if (platform !== 'macos' && platform !== 'windows') {
    throw new Error(`platform debe ser macos o windows: ${configurationPath}`);
  }
  const secretsDirectory = path.dirname(configurationPath);
  const configurationRoot = path.resolve(secretsDirectory, '..');
  process.env.IA_WORKFLOW_AUTHOR_NAME = name;
  process.env.IA_WORKFLOW_AUTHOR_INITIALS = initials;
  process.env.IA_WORKFLOW_AUTHOR_EMAIL = email;
  process.env.IA_WORKFLOW_PLATFORM = platform;
  const projectRoot = configurationRoot;
  const iaRoot = path.join(projectRoot, 'ia');
  return { projectRoot, iaRoot };
}

/** Resuelve { projectRoot, iaRoot } desde argv, igual que WorkflowPaths.FromArgs. */
export function workflowPathsFromArgs(argv = process.argv.slice(2)) {
  const projectRootArg = argFlag(argv, '--project-root');
  const iaRootArg = argFlag(argv, '--ia-root');
  const runtime = argFlag(argv, '--runtime');
  const localSecretsFile = argFlag(argv, '--local-secrets-file');

  if ((runtime ?? '').toLowerCase() === 'studio') {
    return fromStudioConfiguration(projectRootArg, localSecretsFile);
  }

  const projectRoot = path.resolve(projectRootArg ?? process.env.IA_MCP_PROJECT_ROOT ?? process.cwd());
  let iaRoot = iaRootArg ?? process.env.IA_MCP_IA_ROOT;
  if (!iaRoot) {
    iaRoot = path.basename(projectRoot.replace(/[\\/]+$/, '')) === 'ia' ? projectRoot : path.join(projectRoot, 'ia');
  } else {
    iaRoot = path.resolve(iaRoot);
  }
  if (!existsSync(projectRoot)) {
    throw new Error(`raíz de proyecto no encontrada: ${projectRoot}`);
  }
  return { projectRoot, iaRoot };
}
