import { getWorkflow } from '../src/common.mjs';

export default {
  name: 'work_task',
  description: 'Inicia una tarea aprobada o la bloquea/reanuda con un motivo. La vista previa es predeterminada.',
  inputSchema: {
    type: 'object',
    properties: {
      id: { type: 'string', description: 'TASK-ID.' },
      transition: { type: 'string', description: 'blocked or resumed; omit to start.' },
      reason: { type: 'string', description: 'Required for blocked or resumed.' },
      apply: { type: 'boolean', description: 'Set true to apply.' },
    },
    required: ['id'],
    additionalProperties: false,
  },
  async handler(args) {
    return getWorkflow().workTask(args.id, args.transition, args.reason, args.apply === true);
  },
  async smoke({ callTool, check, toolJson }) {
    const res = toolJson(await callTool('work_task', { id: 'TASK-ZZ-MCP-999' }));
    check('work_task con id inexistente responde error', typeof res.error === 'string', res.error);
  },
};
