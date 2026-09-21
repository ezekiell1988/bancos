import { getWorkflow } from '../src/common.mjs';

export default {
  name: 'archive_progress',
  description: 'Archiva las secciones de progreso cerradas mientras conserva el trabajo activo. La vista previa es predeterminada.',
  inputSchema: {
    type: 'object',
    properties: {
      keepDays: { type: 'integer', description: 'Keep closed entries newer than this count of days.' },
      archiveAllClosed: { type: 'boolean', description: 'Archive all closed sections.' },
      apply: { type: 'boolean', description: 'Set true to apply.' },
    },
    required: [],
    additionalProperties: false,
  },
  async handler(args) {
    return getWorkflow().archiveProgress(args.keepDays, args.archiveAllClosed, args.apply === true);
  },
  async smoke({ callTool, check, toolJson }) {
    const res = toolJson(await callTool('archive_progress', {}));
    check('archive_progress responde sin archivo elegible', typeof res.message === 'string', res.message);
  },
};
