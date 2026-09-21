import { getWorkflow } from '../src/common.mjs';

export default {
  name: 'assign_task_to_phase',
  description: 'Asigna una tarea a una fase activa del plan sin editar manualmente 03_plan.md. La vista previa es predeterminada.',
  inputSchema: {
    type: 'object',
    properties: {
      id: { type: 'string', description: 'TASK-ID.' },
      phase: { type: 'string', description: 'Exact phase heading without status.' },
      component: { type: 'string', description: 'Component label.' },
      apply: { type: 'boolean', description: 'Set true to apply.' },
    },
    required: ['id', 'phase', 'component'],
    additionalProperties: false,
  },
  async handler(args) {
    return getWorkflow().assignTaskToPhase(args.id, args.phase, args.component, args.apply === true);
  },
  async smoke({ callTool, check, toolJson }) {
    const res = toolJson(await callTool('assign_task_to_phase', { id: 'TASK-ZZ-MCP-999', phase: 'Fase inexistente', component: 'x' }));
    check('assign_task_to_phase con id inexistente responde error', typeof res.error === 'string', res.error);
  },
};
