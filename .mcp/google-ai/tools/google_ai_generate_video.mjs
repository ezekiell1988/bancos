import { mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import path from 'node:path';
import { getProjectRoot, ToolError } from '../src/common.mjs';
import { resolveWorkspacePath } from '../src/workspace-paths.mjs';
import { createVideoJob, pollVideoOperation, downloadVideoFromOperation } from '../src/client.mjs';

const MIME_BY_EXTENSION = {
  '.png': 'image/png',
  '.jpg': 'image/jpeg',
  '.jpeg': 'image/jpeg',
  '.webp': 'image/webp',
};

export default {
  name: 'google_ai_generate_video',
  description:
    'Genera un video con Veo 3.1 (o Veo 3.1 Fast) a partir de un prompt de texto, opcionalmente usando una imagen de referencia del workspace (imagePath) como primer frame para preservar la identidad visual de un personaje/mascota. Maneja el ciclo completo: creación del job, polling asíncrono y descarga del MP4 en outputPath.',
  inputSchema: {
    type: 'object',
    properties: {
      prompt: { type: 'string', description: 'Descripción cinematográfica detallada de la escena, pose o loop a generar.' },
      outputPath: { type: 'string', description: 'Ruta relativa dentro del workspace donde guardar el video MP4 generado (ej: "src/VoiceBot.Web/src/assets/mascot/video/idle.mp4").' },
      imagePath: { type: 'string', description: 'Ruta relativa opcional a una imagen del workspace (PNG/JPG) a usar como frame inicial (imagen→video), preservando identidad visual.' },
      fast: { type: 'boolean', description: 'Si es true (default), usa Veo 3.1 Fast; si es false, usa Veo 3.1 estándar (mayor calidad, más lento/costoso).' },
      model: { type: 'string', description: 'Nombre de modelo explícito, sobreescribe "fast" si se especifica.' },
      aspectRatio: { type: 'string', description: 'Relación de aspecto del video (default: "9:16").' },
      durationSeconds: { type: 'integer', description: 'Duración del video en segundos (default: 8, máximo obligatorio con imagen de referencia).' },
      resolution: { type: 'string', description: 'Resolución del video (default: "720p").' },
      maxWaitSeconds: { type: 'integer', description: 'Tiempo máximo de espera en segundos para el polling del render (default: 300).' },
      pollIntervalSeconds: { type: 'integer', description: 'Intervalo entre consultas de estado en segundos (default: 10).' },
    },
    required: ['prompt', 'outputPath'],
    additionalProperties: false,
  },
  async handler(args) {
    if (!args.prompt) throw new ToolError('prompt es obligatorio.');
    if (!args.outputPath) throw new ToolError('outputPath es obligatorio.');
    const projectRoot = getProjectRoot();
    const resolvedOutput = resolveWorkspacePath(projectRoot, args.outputPath);

    let imageBase64 = null;
    let imageMimeType = 'image/png';
    if (args.imagePath) {
      const resolvedImage = resolveWorkspacePath(projectRoot, args.imagePath, true);
      imageBase64 = readFileSync(resolvedImage).toString('base64');
      imageMimeType = MIME_BY_EXTENSION[path.extname(resolvedImage).toLowerCase()] ?? 'image/png';
    }

    const aspectRatio = args.aspectRatio ?? '9:16';
    const durationSeconds = args.durationSeconds ?? 8;
    const resolution = args.resolution ?? '720p';
    const fast = args.fast !== false;

    const job = await createVideoJob(projectRoot, {
      prompt: args.prompt,
      model: args.model,
      fast,
      imageBase64,
      imageMimeType,
      aspectRatio,
      durationSeconds,
      resolution,
    });

    const completed = await pollVideoOperation(projectRoot, job.operationName, args.maxWaitSeconds ?? 300, args.pollIntervalSeconds ?? 10);
    const videoBytes = await downloadVideoFromOperation(projectRoot, completed);

    mkdirSync(path.dirname(resolvedOutput), { recursive: true });
    writeFileSync(resolvedOutput, videoBytes);

    return {
      status: 'saved',
      operationName: job.operationName,
      outputPath: args.outputPath,
      resolvedPath: resolvedOutput,
      sizeBytes: videoBytes.length,
      referenceImage: args.imagePath ?? null,
      durationSeconds,
      aspectRatio,
      resolution,
      model: job.model,
    };
  },
  async smoke({ callTool, check, toolJson }) {
    const missing = toolJson(await callTool('google_ai_generate_video', { prompt: 'smoke', outputPath: '../outside.mp4' }));
    check('google_ai_generate_video rechaza outputPath fuera del workspace', typeof missing.error === 'string', missing.error);
  },
};
