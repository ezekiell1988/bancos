import { getWorkflow } from '../src/common.mjs';

export default {
  name: 'reopen_task',
  description: 'Reabre una tarea archivada solo cuando un issue abierto invalida su cierre; después enlaza ambos registros.',
  inputSchema: {
    type: 'object',
    properties: {
      id: { type: 'string', description: 'Archived TASK-ID.' },
      issueId: { type: 'string', description: 'Open ISSUE-ID that invalidates closure.' },
      reason: { type: 'string', description: 'Why closure is invalid.' },
      apply: { type: 'boolean', description: 'Set true to apply.' },
    },
    required: ['id', 'issueId', 'reason'],
    additionalProperties: false,
  },
  async handler(args) {
    return getWorkflow().reopenTask(args.id, args.issueId, args.reason, args.apply === true);
  },
  async smoke({ callTool, check, toolJson }) {
    const res = toolJson(await callTool('reopen_task', { id: 'TASK-ZZ-MCP-999', issueId: 'ISSUE-999', reason: 'smoke' }));
    check('reopen_task con datos inexistentes responde error', typeof res.error === 'string', res.error);
  },
};
