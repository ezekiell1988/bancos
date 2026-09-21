import { getWorkflow } from '../src/common.mjs';

export default {
  name: 'ia_add_progress_entry',
  description: 'Agrega un avance conciso al registro actual y, opcionalmente, al archivo de un componente. La vista previa es predeterminada.',
  inputSchema: {
    type: 'object',
    properties: {
      text: { type: 'string', description: 'Progress entry.' },
      component: { type: 'string', description: 'Optional component.' },
      authorInitials: { type: 'string', description: 'Optional author initials.' },
      apply: { type: 'boolean', description: 'Set true to apply.' },
    },
    required: ['text'],
    additionalProperties: false,
  },
  async handler(args) {
    return getWorkflow().addProgressEntry(args.text, args.component, args.authorInitials, args.apply === true);
  },
  async smoke({ callTool, check, toolJson }) {
    const res = toolJson(await callTool('ia_add_progress_entry', { text: 'smoke test entry' }));
    check('ia_add_progress_entry en preview no escribe', res.preview === true && res.applied === false, JSON.stringify(res).slice(0, 150));
  },
};
