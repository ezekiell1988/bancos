// Puerto de AzureClient.cs + AzureAiVideoTool.cs: cliente REST para Azure AI Foundry v1
// (azureOpenAIEndpoint) usando fetch nativo. Autenticación: header api-key.
import { ToolError } from '../../_shared/results.mjs';
import { loadCredentials, modelFor } from './credentials.mjs';

export function getCredentials(projectRoot) {
  return loadCredentials(projectRoot);
}

async function send(creds, url, init) {
  const response = await fetch(url, { ...init, headers: { ...init.headers, 'api-key': creds.apiKey } });
  if (!response.ok) {
    throw new ToolError(`Azure AI API error (${response.status}): ${await response.text()}`);
  }
  return response.json();
}

export async function chat(projectRoot, { prompt, systemPrompt, model, maxCompletionTokens }) {
  const creds = loadCredentials(projectRoot);
  const messages = [];
  if (systemPrompt) messages.push({ role: 'system', content: systemPrompt });
  messages.push({ role: 'user', content: prompt });

  const targetModel = model ?? modelFor(creds, 'llm', 'gpt-5.5');
  const json = await send(creds, `${creds.endpoint}/chat/completions`, {
    method: 'POST',
    headers: { 'content-type': 'application/json' },
    body: JSON.stringify({ model: targetModel, messages, max_completion_tokens: Math.max(maxCompletionTokens ?? 2000, 100) }),
  });

  const choice = json.choices?.[0];
  return {
    content: choice?.message?.content ?? '',
    model: json.model ?? targetModel,
    usage: json.usage ?? undefined,
    finishReason: choice?.finish_reason ?? null,
  };
}

function decodeImageResult(json, model) {
  const item = json.data?.[0];
  if (!item?.b64_json) throw new ToolError(`Respuesta de imagen sin b64_json: ${JSON.stringify(json)}`);
  return { bytes: Buffer.from(item.b64_json, 'base64'), revised: item.revised_prompt ?? '', model };
}

export async function generateImage(projectRoot, { prompt, model, size, quality, outputFormat }) {
  const creds = loadCredentials(projectRoot);
  const targetModel = model ?? modelFor(creds, 'imageGeneration', 'gpt-image-2');
  const json = await send(creds, `${creds.endpoint}/images/generations`, {
    method: 'POST',
    headers: { 'content-type': 'application/json' },
    body: JSON.stringify({ model: targetModel, prompt, n: 1, size, quality, output_format: outputFormat }),
  });
  return decodeImageResult(json, targetModel);
}

export async function editImage(projectRoot, { imageBytes, imageName, prompt, model, size, quality, inputFidelity, maskBytes }) {
  const creds = loadCredentials(projectRoot);
  const targetModel = model ?? modelFor(creds, 'imageGeneration', 'gpt-image-2');

  const form = new FormData();
  form.set('model', targetModel);
  form.set('prompt', prompt);
  form.set('size', size);
  form.set('quality', quality);
  form.set('input_fidelity', inputFidelity);
  form.set('image', new Blob([imageBytes], { type: imageMimeType(imageName) }), imageName);
  if (maskBytes) form.set('mask', new Blob([maskBytes], { type: 'image/png' }), 'mask.png');

  const json = await send(creds, `${creds.endpoint}/images/edits`, { method: 'POST', body: form });
  return decodeImageResult(json, targetModel);
}

function imageMimeType(fileName) {
  const ext = String(fileName).toLowerCase().split('.').pop();
  if (ext === 'jpg' || ext === 'jpeg') return 'image/jpeg';
  if (ext === 'webp') return 'image/webp';
  return 'image/png';
}

export async function checkEndpoint() {
  return { reachable: true };
}

function sleep(ms) {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

export async function createVideoJob(projectRoot, { prompt, model, seconds, size }) {
  const creds = loadCredentials(projectRoot);
  const targetModel = model ?? modelFor(creds, 'videoGeneration', 'sora-2');
  const json = await send(creds, `${creds.endpoint}/videos`, {
    method: 'POST',
    headers: { 'content-type': 'application/json' },
    body: JSON.stringify({ model: targetModel, prompt, seconds, size }),
  });
  if (typeof json.id !== 'string') throw new ToolError(`Respuesta de video sin "id": ${JSON.stringify(json)}`);
  return { jobId: json.id, model: targetModel };
}

export async function pollVideoJob(projectRoot, jobId, maxWaitSeconds, pollIntervalSeconds) {
  const creds = loadCredentials(projectRoot);
  const until = Date.now() + maxWaitSeconds * 1000;
  const intervalMs = Math.max(pollIntervalSeconds ?? 5, 2) * 1000;

  while (Date.now() < until) {
    const status = await send(creds, `${creds.endpoint}/videos/${jobId}`, { method: 'GET' });
    const state = status.status;
    if (state === 'completed' || state === 'succeeded') return status;
    if (state === 'failed' || state === 'canceled') throw new ToolError(`Generación de video falló con estado "${state}"`);
    await sleep(intervalMs);
  }

  throw new ToolError('Tiempo de espera agotado esperando el video job.');
}

export async function downloadVideo(projectRoot, jobId) {
  const creds = loadCredentials(projectRoot);
  const response = await fetch(`${creds.endpoint}/videos/${jobId}/content`, { headers: { 'api-key': creds.apiKey } });
  if (!response.ok) throw new ToolError(`Error al descargar video (${response.status}): ${await response.text()}`);
  return Buffer.from(await response.arrayBuffer());
}
