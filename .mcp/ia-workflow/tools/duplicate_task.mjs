import { getWorkflow } from '../src/common.mjs';

export default {
  name: 'duplicate_task',
  description: 'Archiva una tarea activa como duplicada después de validar su reemplazo canónico. La vista previa es predeterminada.',
  inputSchema: {
    type: 'object',
    properties: {
      id: { type: 'string', description: 'Active TASK-ID.' },
      duplicateOf: { type: 'string', description: 'Canonical replacement TASK-ID.' },
      reason: { type: 'string', description: 'Reason for duplication.' },
      apply: { type: 'boolean', description: 'Set true to apply.' },
    },
    required: ['id', 'duplicateOf', 'reason'],
    additionalProperties: false,
  },
  async handler(args) {
    return getWorkflow().duplicateTask(args.id, args.duplicateOf, args.reason, args.apply === true);
  },
  async smoke({ callTool, check, toolJson }) {
    const res = toolJson(await callTool('duplicate_task', { id: 'TASK-ZZ-MCP-999', duplicateOf: 'TASK-ZZ-MCP-998', reason: 'smoke' }));
    check('duplicate_task con id inexistente responde error', typeof res.error === 'string', res.error);
  },
};
