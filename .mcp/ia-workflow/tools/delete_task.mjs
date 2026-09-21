import { getWorkflow } from '../src/common.mjs';

export default {
  name: 'delete_task',
  description: 'Archiva una tarea activa como Eliminada con un evento de auditoría. Requiere confirm=true y apply=true.',
  inputSchema: {
    type: 'object',
    properties: {
      id: { type: 'string', description: 'Active TASK-ID.' },
      reason: { type: 'string', description: 'Why deletion is appropriate.' },
      confirm: { type: 'boolean', description: 'Explicit destructive confirmation.' },
      apply: { type: 'boolean', description: 'Set true to apply.' },
    },
    required: ['id', 'reason', 'confirm'],
    additionalProperties: false,
  },
  async handler(args) {
    return getWorkflow().deleteTask(args.id, args.reason, args.confirm, args.apply === true);
  },
  async smoke({ callTool, check, toolJson }) {
    const res = toolJson(await callTool('delete_task', { id: 'TASK-ZZ-MCP-999', reason: 'smoke', confirm: false }));
    check('delete_task sin confirm responde error', typeof res.error === 'string', res.error);
  },
};
