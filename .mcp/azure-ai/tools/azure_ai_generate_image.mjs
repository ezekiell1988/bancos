import { mkdirSync, writeFileSync } from 'node:fs';
import path from 'node:path';
import { getProjectRoot, ToolError } from '../src/common.mjs';
import { resolveWorkspacePath } from '../src/workspace-paths.mjs';
import { generateImage } from '../src/client.mjs';

export default {
  name: 'azure_ai_generate_image',
  description: 'Genera una imagen con gpt-image-2 y guarda el resultado en outputPath.',
  inputSchema: {
    type: 'object',
    properties: {
      prompt: { type: 'string', description: 'Descripción detallada.' },
      outputPath: { type: 'string', description: 'Ruta PNG/JPEG de salida.' },
      model: { type: 'string', description: 'Modelo.' },
      size: { type: 'string', description: 'Tamaño (default: 1024x1024).' },
      quality: { type: 'string', description: 'Calidad (default: medium).' },
      outputFormat: { type: 'string', description: 'Formato (default: png).' },
    },
    required: ['prompt', 'outputPath'],
    additionalProperties: false,
  },
  async handler(args) {
    if (!args.prompt) throw new ToolError('prompt es obligatorio.');
    if (!args.outputPath) throw new ToolError('outputPath es obligatorio.');
    const projectRoot = getProjectRoot();
    const resolved = resolveWorkspacePath(projectRoot, args.outputPath);

    const size = args.size ?? '1024x1024';
    const quality = args.quality ?? 'medium';
    const result = await generateImage(projectRoot, { prompt: args.prompt, model: args.model, size, quality, outputFormat: args.outputFormat ?? 'png' });

    mkdirSync(path.dirname(resolved), { recursive: true });
    writeFileSync(resolved, result.bytes);

    return {
      status: 'saved',
      outputPath: args.outputPath,
      resolvedPath: resolved,
      sizeBytes: result.bytes.length,
      model: result.model,
      size,
      quality,
      revisedPrompt: result.revised,
    };
  },
  async smoke({ callTool, check, toolJson }) {
    const missing = toolJson(await callTool('azure_ai_generate_image', { prompt: 'smoke', outputPath: '../outside.png' }));
    check('azure_ai_generate_image rechaza outputPath fuera del workspace', typeof missing.error === 'string', missing.error);
  },
};
