import { getWorkflow } from '../src/common.mjs';

export default {
  name: 'close_issue',
  description: 'Archiva un issue abierto y registra su resolución en el progreso. La vista previa es predeterminada.',
  inputSchema: {
    type: 'object',
    properties: {
      id: { type: 'string', description: 'Open ISSUE-ID.' },
      resolution: { type: 'string', description: 'Resolution evidence.' },
      component: { type: 'string', description: 'Optional progress component.' },
      apply: { type: 'boolean', description: 'Set true to apply.' },
    },
    required: ['id', 'resolution'],
    additionalProperties: false,
  },
  async handler(args) {
    return getWorkflow().closeIssue(args.id, args.resolution, args.component, args.apply === true);
  },
  async smoke({ callTool, check, toolJson }) {
    const res = toolJson(await callTool('close_issue', { id: 'ISSUE-999999', resolution: 'smoke' }));
    check('close_issue con id inexistente responde error', typeof res.error === 'string', res.error);
  },
};
