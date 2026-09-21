import { getWorkflow } from '../src/common.mjs';

export default {
  name: 'return_task_to_draft',
  description: 'Devuelve una tarea Lista, En progreso o Bloqueada a Borrador y conserva el historial de eventos V2.',
  inputSchema: {
    type: 'object',
    properties: {
      id: { type: 'string', description: 'TASK-ID.' },
      reason: { type: 'string', description: 'Why the work returns to draft.' },
      apply: { type: 'boolean', description: 'Set true to apply.' },
    },
    required: ['id', 'reason'],
    additionalProperties: false,
  },
  async handler(args) {
    return getWorkflow().returnTaskToDraft(args.id, args.reason, args.apply === true);
  },
  async smoke({ callTool, check, toolJson }) {
    const res = toolJson(await callTool('return_task_to_draft', { id: 'TASK-ZZ-MCP-999', reason: 'smoke' }));
    check('return_task_to_draft con id inexistente responde error', typeof res.error === 'string', res.error);
  },
};
