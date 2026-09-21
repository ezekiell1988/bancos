import { getWorkflow } from '../src/common.mjs';

export default {
  name: 'ia_link_issue_to_task',
  description: 'Vincula en ambas direcciones un issue abierto y una tarea activa. La vista previa es predeterminada.',
  inputSchema: {
    type: 'object',
    properties: {
      taskId: { type: 'string', description: 'Active TASK-ID.' },
      issueId: { type: 'string', description: 'Open ISSUE-ID.' },
      reason: { type: 'string', description: 'Why the records are linked.' },
      apply: { type: 'boolean', description: 'Set true to apply.' },
    },
    required: ['taskId', 'issueId', 'reason'],
    additionalProperties: false,
  },
  async handler(args) {
    return getWorkflow().linkIssueToTask(args.taskId, args.issueId, args.reason, args.apply === true);
  },
  async smoke({ callTool, check, toolJson }) {
    const res = toolJson(await callTool('ia_link_issue_to_task', { taskId: 'TASK-ZZ-MCP-999', issueId: 'ISSUE-999999', reason: 'smoke' }));
    check('ia_link_issue_to_task con datos inexistentes responde error', typeof res.error === 'string', res.error);
  },
};
