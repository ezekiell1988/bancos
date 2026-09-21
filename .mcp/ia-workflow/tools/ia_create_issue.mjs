import { getWorkflow } from '../src/common.mjs';

export default {
  name: 'ia_create_issue',
  description: 'Crea un issue abierto y actualiza su índice. La vista previa es predeterminada.',
  inputSchema: {
    type: 'object',
    properties: {
      title: { type: 'string', description: 'Issue title.' },
      severity: { type: 'string', description: 'low, medium, high or critical.' },
      component: { type: 'string', description: 'Affected component.' },
      symptom: { type: 'string', description: 'Observed symptom.' },
      apply: { type: 'boolean', description: 'Set true to apply.' },
    },
    required: ['title', 'severity', 'component', 'symptom'],
    additionalProperties: false,
  },
  async handler(args) {
    return getWorkflow().createIssue(args.title, args.severity, args.component, args.symptom, args.apply === true);
  },
  async smoke({ callTool, check, toolJson }) {
    const res = toolJson(
      await callTool('ia_create_issue', { title: 'Smoke issue', severity: 'low', component: 'mcp', symptom: 'smoke test' }),
    );
    check('ia_create_issue en preview no escribe', res.preview === true && res.applied === false, JSON.stringify(res).slice(0, 150));
  },
};
