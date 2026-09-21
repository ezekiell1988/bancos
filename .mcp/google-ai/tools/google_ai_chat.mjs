import { mkdirSync, writeFileSync } from 'node:fs';
import path from 'node:path';
import { getProjectRoot, ToolError } from '../src/common.mjs';
import { resolveWorkspacePath } from '../src/workspace-paths.mjs';
import { generateContent } from '../src/client.mjs';

export default {
  name: 'google_ai_chat',
  description:
    'Genera texto con un modelo Gemini (gemini-2.5-flash-lite por defecto, u otro configurado en ai-google.json). Si se especifica outputPath, guarda la respuesta en el archivo indicado dentro del workspace.',
  inputSchema: {
    type: 'object',
    properties: {
      prompt: { type: 'string', description: 'Mensaje o consulta principal del usuario.' },
      systemInstruction: { type: 'string', description: 'Instrucción de sistema/rol (opcional).' },
      model: { type: 'string', description: 'Nombre del modelo Gemini a usar (default: el defaultModel de ai-google.json).' },
      temperature: { type: 'number', description: 'Temperatura de muestreo (opcional).' },
      maxOutputTokens: { type: 'integer', description: 'Límite de tokens de salida (opcional).' },
      outputPath: { type: 'string', description: 'Ruta relativa dentro del workspace donde guardar el texto de la respuesta.' },
    },
    required: ['prompt'],
    additionalProperties: false,
  },
  async handler(args) {
    if (!args.prompt) throw new ToolError('prompt es obligatorio.');
    const projectRoot = getProjectRoot();
    const result = await generateContent(projectRoot, {
      prompt: args.prompt,
      systemInstruction: args.systemInstruction,
      model: args.model,
      temperature: args.temperature,
      maxOutputTokens: args.maxOutputTokens,
    });

    let savedFile = null;
    if (args.outputPath) {
      const resolved = resolveWorkspacePath(projectRoot, args.outputPath);
      mkdirSync(path.dirname(resolved), { recursive: true });
      writeFileSync(resolved, result.content, 'utf8');
      savedFile = args.outputPath;
    }

    return { model: result.model, content: result.content, finishReason: result.finishReason, usage: result.usage, savedFile };
  },
  async smoke({ callTool, check, toolJson }) {
    const missing = toolJson(await callTool('google_ai_chat', {}));
    check('google_ai_chat requiere prompt', typeof missing.error === 'string', missing.error);
  },
};
