import { getWorkflow } from '../src/common.mjs';

export default {
  name: 'finish_task',
  description: 'Archiva una tarea En progreso, actualiza su aporte exacto al plan y crea indicadores de revisión solo cuando la fase está completa.',
  inputSchema: {
    type: 'object',
    properties: {
      id: { type: 'string', description: 'TASK-ID.' },
      summary: { type: 'string', description: 'Short completion summary.' },
      apply: { type: 'boolean', description: 'Set true to apply.' },
    },
    required: ['id'],
    additionalProperties: false,
  },
  async handler(args) {
    return getWorkflow().finishTask(args.id, args.summary, args.apply === true);
  },
  async smoke({ callTool, check, toolJson }) {
    const res = toolJson(await callTool('finish_task', { id: 'TASK-ZZ-MCP-999' }));
    check('finish_task con id inexistente responde error', typeof res.error === 'string', res.error);
  },
};
