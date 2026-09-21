// Puerto de GoogleAiClient.cs: cliente REST para Gemini API (generativelanguage.googleapis.com/v1beta)
// usando fetch nativo. Autenticación: header x-goog-api-key (misma apiKey para texto y video).
// Referencia (ADR-103): la imagen de referencia va en "image": { bytesBase64Encoded, mimeType }
// dentro de cada "instance" — NO "inlineData" (rechazado con HTTP 400 por el modelo).
import { ToolError } from '../../_shared/results.mjs';
import { loadCredentials } from './credentials.mjs';

export function getCredentials(projectRoot) {
  return loadCredentials(projectRoot);
}

export async function generateContent(projectRoot, { prompt, systemInstruction, model, temperature, maxOutputTokens }) {
  const creds = loadCredentials(projectRoot);
  const targetModel = model ?? creds.models.llm;
  const url = `${creds.endpoint}/models/${targetModel}:generateContent`;

  const body = { contents: [{ role: 'user', parts: [{ text: prompt }] }] };
  if (systemInstruction) body.systemInstruction = { parts: [{ text: systemInstruction }] };

  const generationConfig = {};
  if (temperature !== undefined && temperature !== null) generationConfig.temperature = temperature;
  if (maxOutputTokens !== undefined && maxOutputTokens !== null) generationConfig.maxOutputTokens = maxOutputTokens;
  if (Object.keys(generationConfig).length > 0) body.generationConfig = generationConfig;

  const response = await fetch(url, {
    method: 'POST',
    headers: { 'content-type': 'application/json', 'x-goog-api-key': creds.apiKey },
    body: JSON.stringify(body),
  });
  if (!response.ok) {
    throw new ToolError(`Gemini generateContent error (${response.status}): ${await response.text()}`);
  }

  const json = await response.json();
  const candidate = Array.isArray(json.candidates) && json.candidates.length > 0 ? json.candidates[0] : null;

  let text = '';
  let finishReason = null;
  if (candidate && typeof candidate === 'object') {
    finishReason = candidate.finishReason ?? null;
    const parts = candidate.content?.parts;
    if (Array.isArray(parts)) text = parts.map((part) => part.text ?? '').join('');
  }

  const usage = json.usageMetadata ?? {};
  return { content: text, model: targetModel, finishReason, usage };
}

export async function createVideoJob(projectRoot, { prompt, model, fast, imageBase64, imageMimeType, aspectRatio, durationSeconds, resolution }) {
  const creds = loadCredentials(projectRoot);
  const targetModel = model ?? (fast ? creds.models.videoFast : creds.models.video);
  const url = `${creds.endpoint}/models/${targetModel}:predictLongRunning`;

  const instance = { prompt };
  if (imageBase64 !== null && imageBase64 !== undefined) {
    instance.image = { bytesBase64Encoded: imageBase64, mimeType: imageMimeType };
  }

  const body = { instances: [instance], parameters: { aspectRatio, durationSeconds, resolution } };

  const response = await fetch(url, {
    method: 'POST',
    headers: { 'content-type': 'application/json', 'x-goog-api-key': creds.apiKey },
    body: JSON.stringify(body),
  });
  if (!response.ok) {
    throw new ToolError(`Veo predictLongRunning error (${response.status}): ${await response.text()}`);
  }

  const json = await response.json();
  if (typeof json.name !== 'string') {
    throw new ToolError(`Respuesta de Veo sin "name" de operación: ${JSON.stringify(json)}`);
  }

  return { operationName: json.name, model: targetModel };
}

export async function getVideoOperationStatus(projectRoot, operationName) {
  const creds = loadCredentials(projectRoot);
  const response = await fetch(`${creds.endpoint}/${operationName}`, {
    headers: { 'x-goog-api-key': creds.apiKey },
  });
  if (!response.ok) {
    throw new ToolError(`Error consultando operación de video ${operationName} (${response.status}): ${await response.text()}`);
  }
  return response.json();
}

function sleep(ms) {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

export async function pollVideoOperation(projectRoot, operationName, maxWaitSeconds, intervalSeconds) {
  const start = Date.now();
  const maxSpanMs = maxWaitSeconds * 1000;
  const intervalMs = Math.max(intervalSeconds, 5) * 1000;

  while (Date.now() - start < maxSpanMs) {
    const status = await getVideoOperationStatus(projectRoot, operationName);

    if (status.error !== undefined && status.error !== null) {
      throw new ToolError(`Generación de video Veo falló: ${JSON.stringify(status.error)}`);
    }
    if (status.done === true) return status;

    await sleep(intervalMs);
  }

  throw new ToolError(`Tiempo de espera agotado (${maxWaitSeconds}s) esperando la operación de video ${operationName}.`);
}

function findVideoUri(node) {
  if (Array.isArray(node)) {
    for (const item of node) {
      const found = findVideoUri(item);
      if (found) return found;
    }
    return null;
  }
  if (node && typeof node === 'object') {
    if (typeof node.uri === 'string' && node.uri.length > 0) return node.uri;
    for (const value of Object.values(node)) {
      const found = findVideoUri(value);
      if (found) return found;
    }
    return null;
  }
  return null;
}

export async function downloadVideoFromOperation(projectRoot, operationResponse) {
  const creds = loadCredentials(projectRoot);
  const responseNode = operationResponse.response ?? operationResponse;
  const uri = findVideoUri(responseNode);
  if (!uri) throw new ToolError(`No se encontró una URI de video en la respuesta de la operación: ${JSON.stringify(operationResponse)}`);

  const response = await fetch(uri, { headers: { 'x-goog-api-key': creds.apiKey } });
  if (!response.ok) {
    throw new ToolError(`Error al descargar video Veo (${response.status}): ${await response.text()}`);
  }
  return Buffer.from(await response.arrayBuffer());
}

export async function checkEndpoint(projectRoot) {
  const creds = loadCredentials(projectRoot);
  try {
    const response = await fetch(`${creds.endpoint}/models`, { headers: { 'x-goog-api-key': creds.apiKey } });
    return { reachable: true, status: response.status };
  } catch (err) {
    return { reachable: false, status: err.message };
  }
}
