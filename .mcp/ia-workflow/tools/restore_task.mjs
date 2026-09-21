import { getWorkflow } from '../src/common.mjs';

export default {
  name: 'restore_task',
  description: 'Copia una tarea archivada de vuelta a Borrador mientras conserva su fuente histórica archivada.',
  inputSchema: {
    type: 'object',
    properties: {
      id: { type: 'string', description: 'Archived TASK-ID.' },
      reason: { type: 'string', description: 'Why the archived task is restored.' },
      apply: { type: 'boolean', description: 'Set true to apply.' },
    },
    required: ['id', 'reason'],
    additionalProperties: false,
  },
  async handler(args) {
    return getWorkflow().restoreTask(args.id, args.reason, args.apply === true);
  },
  async smoke({ callTool, check, toolJson }) {
    const res = toolJson(await callTool('restore_task', { id: 'TASK-ZZ-MCP-999', reason: 'smoke' }));
    check('restore_task con id inexistente responde error', typeof res.error === 'string', res.error);
  },
};
