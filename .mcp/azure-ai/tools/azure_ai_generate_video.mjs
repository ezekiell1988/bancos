import { mkdirSync, writeFileSync } from 'node:fs';
import path from 'node:path';
import { getProjectRoot, ToolError } from '../src/common.mjs';
import { resolveWorkspacePath } from '../src/workspace-paths.mjs';
import { createVideoJob, pollVideoJob, downloadVideo } from '../src/client.mjs';

export default {
  name: 'azure_ai_generate_video',
  description: 'Genera un video con sora-2, espera el job (polling) y guarda el MP4 en outputPath.',
  inputSchema: {
    type: 'object',
    properties: {
      prompt: { type: 'string', description: 'Prompt cinematográfico.' },
      outputPath: { type: 'string', description: 'MP4 de salida.' },
      seconds: { type: 'string', description: 'Duración como string "4", "8" o "12" (default: "4").' },
      size: { type: 'string', description: 'Resolución (default: "1280x720").' },
      model: { type: 'string', description: 'Modelo.' },
      maxWaitSeconds: { type: 'integer', description: 'Máximo de espera en segundos (default: 300).' },
      pollIntervalSeconds: { type: 'integer', description: 'Intervalo de polling en segundos (default: 5).' },
    },
    required: ['prompt', 'outputPath'],
    additionalProperties: false,
  },
  async handler(args) {
    if (!args.prompt) throw new ToolError('prompt es obligatorio.');
    if (!args.outputPath) throw new ToolError('outputPath es obligatorio.');
    const projectRoot = getProjectRoot();
    const resolved = resolveWorkspacePath(projectRoot, args.outputPath);

    const seconds = args.seconds ?? '4';
    const size = args.size ?? '1280x720';

    const job = await createVideoJob(projectRoot, { prompt: args.prompt, model: args.model, seconds, size });
    await pollVideoJob(projectRoot, job.jobId, args.maxWaitSeconds ?? 300, args.pollIntervalSeconds ?? 5);
    const bytes = await downloadVideo(projectRoot, job.jobId);

    mkdirSync(path.dirname(resolved), { recursive: true });
    writeFileSync(resolved, bytes);

    return {
      status: 'saved',
      jobId: job.jobId,
      outputPath: args.outputPath,
      resolvedPath: resolved,
      sizeBytes: bytes.length,
      durationSeconds: seconds,
      resolution: size,
      model: job.model,
    };
  },
  async smoke({ callTool, check, toolJson }) {
    const missing = toolJson(await callTool('azure_ai_generate_video', { prompt: 'smoke', outputPath: '../outside.mp4' }));
    check('azure_ai_generate_video rechaza outputPath fuera del workspace', typeof missing.error === 'string', missing.error);
  },
};
