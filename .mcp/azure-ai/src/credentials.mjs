// Puerto de AzureCredentialsProvider.cs: carga ai-foundry.json desde .local-secrets/,
// credentials/ o src/demo/credentials/ (mismo orden que Cli.ResolveSecretsFileName + AzurePaths).
import { existsSync, readFileSync } from 'node:fs';
import path from 'node:path';
import { ToolError } from '../../_shared/results.mjs';

const SECRETS_FILE = 'ai-foundry.json';

let cached = null;

function findSecretsFile(projectRoot) {
  const candidates = [
    path.join(projectRoot, '.local-secrets', SECRETS_FILE),
    path.join(projectRoot, 'credentials', SECRETS_FILE),
    path.join(projectRoot, 'src', 'demo', 'credentials', SECRETS_FILE),
  ];
  return candidates.find((candidate) => existsSync(candidate)) ?? null;
}

function getRequiredString(doc, property) {
  const value = doc?.[property];
  if (typeof value !== 'string' || value.trim() === '') throw new ToolError(`credenciales: falta ${property}`);
  return value;
}

/** Carga (con caché en memoria del proceso) las credenciales de Azure AI Foundry. */
export function loadCredentials(projectRoot) {
  if (cached) return cached;

  const file = findSecretsFile(projectRoot);
  if (!file) {
    throw new ToolError(`No se encontró el archivo de credenciales "${SECRETS_FILE}" en .local-secrets/, credentials/ o src/demo/credentials/.`);
  }

  const doc = JSON.parse(readFileSync(file, 'utf8'));

  const apiKey = getRequiredString(doc, 'apiKey');
  if (apiKey === 'YOUR_API_KEY_HERE') throw new ToolError('apiKey inválida');

  const endpoint = getRequiredString(doc, 'azureOpenAIEndpoint').replace(/\/+$/, '');
  const projectEndpoint = typeof doc.projectEndpoint === 'string' ? doc.projectEndpoint.replace(/\/+$/, '') : null;

  const models = {};
  if (doc.models && typeof doc.models === 'object') {
    for (const [key, value] of Object.entries(doc.models)) {
      if (typeof value === 'string') models[key] = value;
    }
  }

  cached = { apiKey, endpoint, projectEndpoint, models, sourcePath: path.relative(projectRoot, file) };
  return cached;
}

export function modelFor(creds, key, fallback) {
  return creds.models[key] ?? fallback;
}
