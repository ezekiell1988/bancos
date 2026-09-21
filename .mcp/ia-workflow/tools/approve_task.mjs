import { getWorkflow } from '../src/common.mjs';

export default {
  name: 'approve_task',
  description: 'Mueve una tarea de Borrador a Lista después de validarla. El trabajo de alto riesgo requiere un aprobador explícito.',
  inputSchema: {
    type: 'object',
    properties: {
      id: { type: 'string', description: 'TASK-ID.' },
      approver: { type: 'string', description: 'Explicit approver, required for risk alto.' },
      apply: { type: 'boolean', description: 'Set true to apply.' },
    },
    required: ['id'],
    additionalProperties: false,
  },
  async handler(args) {
    return getWorkflow().approveTask(args.id, args.approver, args.apply === true);
  },
  async smoke({ callTool, check, toolJson }) {
    const res = toolJson(await callTool('approve_task', { id: 'TASK-ZZ-MCP-999' }));
    check('approve_task con id inexistente responde error', typeof res.error === 'string', res.error);
  },
};
