import { getWorkflow } from '../src/common.mjs';

export default {
  name: 'ia_inspect',
  order: 2,
  description:
    'Enruta una lectura puntual por acción: list_tasks, read_task, list_decisions, read_decision, list_issues, list_pending_devops_link, read_file, search, metrics o migration. Cada acción acepta solo sus propios campos.',
  inputSchema: {
    type: 'object',
    properties: {
      action: {
        type: 'string',
        description: 'list_tasks, read_task, list_decisions, read_decision, list_issues, list_pending_devops_link, read_file, search, metrics or migration.',
      },
      status: { type: 'string' },
      mode: { type: 'string' },
      user: { type: 'string' },
      id: { type: 'string' },
      maxChars: { type: 'integer' },
      query: { type: 'string' },
      includeText: { type: 'boolean' },
      path: { type: 'string' },
      scope: { type: 'string' },
      maxResults: { type: 'integer' },
      contextLines: { type: 'integer' },
      from: { type: 'string' },
      asOf: { type: 'string' },
      filter: { type: 'string' },
      targetCount: { type: 'integer' },
      seed: { type: 'integer' },
    },
    required: ['action'],
    additionalProperties: false,
  },
  async handler(args) {
    return getWorkflow().inspect(args);
  },
  async smoke({ callTool, check, toolJson }) {
    const res = toolJson(await callTool('ia_inspect', { action: 'list_tasks', mode: 'pathsOnly' }));
    check('ia_inspect list_tasks devuelve groups', typeof res.groups === 'object', JSON.stringify(res).slice(0, 150));

    const err = toolJson(await callTool('ia_inspect', { action: 'list_tasks', query: 'no permitido' }));
    check('ia_inspect rechaza parámetros de otra acción', typeof err.error === 'string', err.error);
  },
};
