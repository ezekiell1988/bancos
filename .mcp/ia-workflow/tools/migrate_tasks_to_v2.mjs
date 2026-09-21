import { getWorkflow } from '../src/common.mjs';

export default {
  name: 'migrate_tasks_to_v2',
  description: 'Reporta candidatos de migración V1 en vista previa y se niega a inventar historial durante la aplicación.',
  inputSchema: {
    type: 'object',
    properties: {
      reason: { type: 'string', description: 'Reason for explicit migration.' },
      apply: { type: 'boolean', description: 'Set true only after reviewing the preview.' },
    },
    required: ['reason'],
    additionalProperties: false,
  },
  async handler(args) {
    return getWorkflow().migrateTasksToV2(args.reason, args.apply === true);
  },
  async smoke({ callTool, check, toolJson }) {
    const res = toolJson(await callTool('migrate_tasks_to_v2', { reason: 'smoke test' }));
    check('migrate_tasks_to_v2 responde preview sin aplicar', res.applied === false || res.applied === undefined, JSON.stringify(res).slice(0, 150));
  },
};
