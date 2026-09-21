// Puerto de GoogleAiCredentialsProvider.cs: carga y fusiona credenciales de texto
// (ai-google.json) y video (ai-google-veo.json). Cualquiera de los dos puede faltar.
import { existsSync, readFileSync } from 'node:fs';
import path from 'node:path';
import { ToolError } from '../../_shared/results.mjs';

const TEXT_SECRETS_FILE = 'ai-google.json';
const VIDEO_SECRETS_FILE = 'ai-google-veo.json';
const DEFAULT_ENDPOINT = 'https://generativelanguage.googleapis.com/v1beta';

let cached = null;

function findSecretsFile(projectRoot, fileName) {
  const candidates = [path.join(projectRoot, '.local-secrets', fileName), path.join(projectRoot, 'credentials', fileName)];
  return candidates.find((candidate) => existsSync(candidate)) ?? null;
}

function getString(doc, property) {
  const value = doc?.[property];
  return typeof value === 'string' ? value : null;
}

function getNestedString(doc, parent, property) {
  const value = doc?.[parent];
  return value && typeof value === 'object' && typeof value[property] === 'string' ? value[property] : null;
}

function getStringArray(doc, property) {
  const value = doc?.[property];
  return Array.isArray(value) ? value.filter((v) => typeof v === 'string') : [];
}

/** Carga (con caché en memoria del proceso) las credenciales fusionadas de texto+video. */
export function loadCredentials(projectRoot) {
  if (cached) return cached;

  const textPath = findSecretsFile(projectRoot, TEXT_SECRETS_FILE);
  const videoPath = findSecretsFile(projectRoot, VIDEO_SECRETS_FILE);

  if (!textPath && !videoPath) {
    throw new ToolError(
      `No se encontraron credenciales de Google AI. Se buscó "${TEXT_SECRETS_FILE}" y "${VIDEO_SECRETS_FILE}" en .local-secrets/ y credentials/.\n` +
        'Crea los archivos siguiendo .local-secrets/ai-google.example.json y .local-secrets/ai-google-veo.example.json.',
    );
  }

  const textDoc = textPath ? JSON.parse(readFileSync(textPath, 'utf8')) : null;
  const videoDoc = videoPath ? JSON.parse(readFileSync(videoPath, 'utf8')) : null;

  const apiKey = (getString(textDoc, 'apiKey') ?? getString(videoDoc, 'apiKey') ?? '').trim();
  if (!apiKey || apiKey === 'AIzaSy...') {
    throw new ToolError(`No se encontró un "apiKey" válido en ${TEXT_SECRETS_FILE} / ${VIDEO_SECRETS_FILE}.`);
  }

  const endpoint = (getString(textDoc, 'endpoint') ?? getString(videoDoc, 'endpoint') ?? DEFAULT_ENDPOINT).replace(/\/+$/, '');

  const models = {
    llm: getString(textDoc, 'defaultModel') ?? 'gemini-2.5-flash-lite',
    textModels: getStringArray(textDoc, 'textModels'),
    video: getNestedString(videoDoc, 'models', 'video') ?? getNestedString(videoDoc, 'videoModels', 'standard') ?? 'veo-3.1-generate-preview',
    videoFast: getNestedString(videoDoc, 'models', 'videoFast') ?? getNestedString(videoDoc, 'videoModels', 'fast') ?? 'veo-3.1-fast-generate-preview',
  };

  const sourcePaths = [];
  if (textPath) sourcePaths.push(path.relative(projectRoot, textPath));
  if (videoPath) sourcePaths.push(path.relative(projectRoot, videoPath));

  cached = { apiKey, endpoint, models, sourcePaths };
  return cached;
}
