import { mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import path from 'node:path';
import { getProjectRoot, ToolError } from '../src/common.mjs';
import { resolveWorkspacePath } from '../src/workspace-paths.mjs';
import { editImage } from '../src/client.mjs';

export default {
  name: 'azure_ai_edit_image',
  description: 'Edita una imagen del workspace con gpt-image-2 y guarda el resultado en outputPath.',
  inputSchema: {
    type: 'object',
    properties: {
      imagePath: { type: 'string', description: 'Imagen de referencia.' },
      prompt: { type: 'string', description: 'Cambio deseado.' },
      outputPath: { type: 'string', description: 'Ruta de salida.' },
      maskPath: { type: 'string', description: 'Máscara opcional.' },
      model: { type: 'string', description: 'Modelo.' },
      size: { type: 'string', description: 'Tamaño (default: 1024x1024).' },
      quality: { type: 'string', description: 'Calidad (default: low).' },
      inputFidelity: { type: 'string', description: 'Fidelidad (default: high).' },
    },
    required: ['imagePath', 'prompt', 'outputPath'],
    additionalProperties: false,
  },
  async handler(args) {
    if (!args.imagePath) throw new ToolError('imagePath es obligatorio.');
    if (!args.prompt) throw new ToolError('prompt es obligatorio.');
    if (!args.outputPath) throw new ToolError('outputPath es obligatorio.');
    const projectRoot = getProjectRoot();

    const input = resolveWorkspacePath(projectRoot, args.imagePath, true);
    const output = resolveWorkspacePath(projectRoot, args.outputPath);
    const maskBytes = args.maskPath ? readFileSync(resolveWorkspacePath(projectRoot, args.maskPath, true)) : null;

    const quality = args.quality ?? 'low';
    const result = await editImage(projectRoot, {
      imageBytes: readFileSync(input),
      imageName: path.basename(input),
      prompt: args.prompt,
      model: args.model,
      size: args.size ?? '1024x1024',
      quality,
      inputFidelity: args.inputFidelity ?? 'high',
      maskBytes,
    });

    mkdirSync(path.dirname(output), { recursive: true });
    writeFileSync(output, result.bytes);

    return {
      status: 'saved',
      referenceImage: args.imagePath,
      outputPath: args.outputPath,
      resolvedPath: output,
      sizeBytes: result.bytes.length,
      model: result.model,
      quality,
      revisedPrompt: result.revised,
    };
  },
  async smoke({ callTool, check, toolJson }) {
    const missing = toolJson(await callTool('azure_ai_edit_image', { imagePath: 'no-existe.png', prompt: 'smoke', outputPath: 'tmp.png' }));
    check('azure_ai_edit_image rechaza imagePath inexistente', typeof missing.error === 'string', missing.error);
  },
};
