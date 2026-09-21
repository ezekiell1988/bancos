import { getWorkflow } from '../src/common.mjs';

export default {
  name: 'ia_get_context',
  order: 1,
  description: 'Devuelve el conjunto mínimo de contexto de /ia para planificar, implementar, revisar, depurar o cerrar_sesion.',
  inputSchema: {
    type: 'object',
    properties: {
      intent: { type: 'string', description: 'Session intent.' },
      taskId: { type: 'string', description: 'Optional TASK-ID to include.' },
      issueId: { type: 'string', description: 'Optional ISSUE-ID to include.' },
      mode: { type: 'string', description: 'full, summary or pathsOnly.' },
      includeText: { type: 'boolean', description: 'Whether to include file text.' },
      maxChars: { type: 'integer', description: 'Maximum characters per full file.' },
    },
    required: ['intent'],
    additionalProperties: false,
  },
  async handler(args) {
    return getWorkflow().getContext(args.intent, args.taskId, args.issueId, args.mode, args.includeText, args.maxChars);
  },
  async smoke({ callTool, check, toolJson }) {
    const res = toolJson(await callTool('ia_get_context', { intent: 'planificar', mode: 'pathsOnly' }));
    check('ia_get_context devuelve files[]', Array.isArray(res.files) && res.files.length > 0, JSON.stringify(res).slice(0, 150));
  },
};
