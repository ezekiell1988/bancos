import { mkdirSync, writeFileSync } from 'node:fs';
import path from 'node:path';
import { getProjectRoot, ToolError } from '../src/common.mjs';
import { resolveWorkspacePath } from '../src/workspace-paths.mjs';
import { chat } from '../src/client.mjs';

export default {
  name: 'azure_ai_chat',
  description: 'Ejecuta Chat Completion con gpt-5.5 (u otro modelo configurado en ai-foundry.json) y opcionalmente guarda la respuesta en outputPath.',
  inputSchema: {
    type: 'object',
    properties: {
      prompt: { type: 'string', description: 'Mensaje principal.' },
      systemPrompt: { type: 'string', description: 'Instrucción de sistema opcional.' },
      model: { type: 'string', description: 'Modelo.' },
      maxCompletionTokens: { type: 'integer', description: 'Máximo de tokens.' },
      outputPath: { type: 'string', description: 'Ruta donde guardar la respuesta.' },
    },
    required: ['prompt'],
    additionalProperties: false,
  },
  async handler(args) {
    if (!args.prompt) throw new ToolError('Debe proporcionar "prompt" o un arreglo "messages" no vacío.');
    const projectRoot = getProjectRoot();
    const result = await chat(projectRoot, {
      prompt: args.prompt,
      systemPrompt: args.systemPrompt,
      model: args.model,
      maxCompletionTokens: args.maxCompletionTokens,
    });

    let savedFile = null;
    if (args.outputPath) {
      const resolved = resolveWorkspacePath(projectRoot, args.outputPath);
      mkdirSync(path.dirname(resolved), { recursive: true });
      writeFileSync(resolved, result.content, 'utf8');
      savedFile = args.outputPath;
    }

    return { model: result.model, content: result.content, usage: result.usage, finishReason: result.finishReason, savedFile };
  },
  async smoke({ callTool, check, toolJson }) {
    const missing = toolJson(await callTool('azure_ai_chat', {}));
    check('azure_ai_chat requiere prompt', typeof missing.error === 'string', missing.error);
  },
};
