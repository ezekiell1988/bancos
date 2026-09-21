import { execFile } from 'node:child_process';
import { existsSync, mkdirSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { promisify } from 'node:util';
import { getProjectRoot, ToolError } from '../src/common.mjs';
import { resolveWorkspacePath } from '../src/workspace-paths.mjs';
import { generateImage, editImage } from '../src/client.mjs';

const execFileAsync = promisify(execFile);
const here = path.dirname(fileURLToPath(import.meta.url));
const mcpRoot = path.join(here, '..');

const GREEN_SCREEN_PROMPT =
  'SOLID PURE CHROMA KEY GREEN BACKGROUND (#00FF00), completely flat and uniform, no gradients, no shadows, no texture. The subject must contain NO green tones, centered, no text, no watermark.';

export default {
  name: 'azure_ai_generate_transparent_image',
  description: 'Genera una imagen sobre fondo chroma key verde con gpt-image-2 y ejecuta el script Python existente (green_screen_cutout.py) para producir un PNG con canal alfa.',
  inputSchema: {
    type: 'object',
    properties: {
      prompt: { type: 'string', description: 'Descripción de la imagen.' },
      outputPath: { type: 'string', description: 'PNG final con transparencia.' },
      referenceImagePath: { type: 'string', description: 'Imagen de referencia opcional (usa edición en vez de generación).' },
      size: { type: 'string', description: 'Tamaño (default: 1024x1024).' },
      quality: { type: 'string', description: 'Calidad (default: medium sin referencia, low con referencia).' },
      inputFidelity: { type: 'string', description: 'Fidelidad si hay referencia (default: high).' },
      keepGreenScreenCopy: { type: 'boolean', description: 'Si es true, conserva el intermedio de chroma key verde.' },
    },
    required: ['prompt', 'outputPath'],
    additionalProperties: false,
  },
  async handler(args) {
    if (!args.prompt) throw new ToolError('prompt es obligatorio.');
    if (!args.outputPath) throw new ToolError('outputPath es obligatorio.');
    const projectRoot = getProjectRoot();
    const final = resolveWorkspacePath(projectRoot, args.outputPath);
    mkdirSync(path.dirname(final), { recursive: true });

    const fullPrompt = `${args.prompt}\n${GREEN_SCREEN_PROMPT}`;
    const size = args.size ?? '1024x1024';

    let result;
    if (!args.referenceImagePath) {
      result = await generateImage(projectRoot, { prompt: fullPrompt, model: null, size, quality: args.quality ?? 'medium', outputFormat: 'png' });
    } else {
      const input = resolveWorkspacePath(projectRoot, args.referenceImagePath, true);
      result = await editImage(projectRoot, {
        imageBytes: readFileSync(input),
        imageName: path.basename(input),
        prompt: fullPrompt,
        model: null,
        size: '1024x1024',
        quality: args.quality ?? 'low',
        inputFidelity: args.inputFidelity ?? 'high',
        maskBytes: null,
      });
    }

    const greenScreenPath = `${final.replace(/\.[^./]+$/, '')}.greenscreen.png`;
    writeFileSync(greenScreenPath, result.bytes);

    const python = existsSync(path.join(projectRoot, '.venv', 'bin', 'python3')) ? path.join(projectRoot, '.venv', 'bin', 'python3') : 'python3';
    const scriptPath = path.join(mcpRoot, 'scripts', 'green_screen_cutout.py');

    let stdout;
    try {
      ({ stdout } = await execFileAsync(python, [scriptPath, greenScreenPath, final]));
    } catch (err) {
      throw new ToolError(`El script de recorte de fondo verde falló: ${err.message}`);
    }

    const lastLine = stdout.trim().split('\n').pop();
    const report = JSON.parse(lastLine);

    if (args.keepGreenScreenCopy !== true) rmSync(greenScreenPath);

    return {
      status: 'completed',
      outputPath: args.outputPath,
      resolvedPath: final,
      greenScreenIntermediatePath: args.keepGreenScreenCopy === true ? greenScreenPath : null,
      model: result.model,
      revisedPrompt: result.revised,
      validation: report,
      warnings: [],
    };
  },
  async smoke({ callTool, check, toolJson }) {
    const missing = toolJson(await callTool('azure_ai_generate_transparent_image', { prompt: 'smoke', outputPath: '../outside.png' }));
    check('azure_ai_generate_transparent_image rechaza outputPath fuera del workspace', typeof missing.error === 'string', missing.error);
  },
};
