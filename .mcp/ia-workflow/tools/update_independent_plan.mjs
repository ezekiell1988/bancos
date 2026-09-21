import { getWorkflow } from '../src/common.mjs';

export default {
  name: 'update_independent_plan',
  description:
    'Actualiza solo estados canónicos de un archivo plan-*.md existente bajo ia/03_plan. No acepta rutas ni contenido arbitrario; la vista previa es predeterminada.',
  inputSchema: {
    type: 'object',
    properties: {
      planFile: { type: 'string', description: 'Nombre exacto plan-*.md dentro de ia/03_plan, sin directorios.' },
      planStatus: { type: 'string', description: 'Estado opcional del plan: open, in_progress o completed.' },
      taskUpdates: {
        type: 'array',
        description: 'Filas existentes a actualizar, con taskId y estado draft, ready, in_progress, completed o blocked.',
        items: {
          type: 'object',
          properties: {
            taskId: { type: 'string' },
            status: { type: 'string' },
          },
          required: ['taskId', 'status'],
          additionalProperties: false,
        },
      },
      phaseUpdates: {
        type: 'array',
        description: 'Fases existentes a actualizar, con phaseTitle y estado planned, in_progress, completed o blocked.',
        items: {
          type: 'object',
          properties: {
            phaseTitle: { type: 'string' },
            status: { type: 'string' },
          },
          required: ['phaseTitle', 'status'],
          additionalProperties: false,
        },
      },
      apply: { type: 'boolean', description: 'Set true to apply. Default only returns a preview.' },
    },
    required: ['planFile'],
    additionalProperties: false,
  },
  async handler(args) {
    return getWorkflow().updateIndependentPlan(args.planFile, args.planStatus, args.taskUpdates, args.phaseUpdates, args.apply === true);
  },
  async smoke({ callTool, check, toolJson }) {
    const res = toolJson(await callTool('update_independent_plan', { planFile: 'plan-inexistente.md', planStatus: 'open' }));
    check('update_independent_plan con plan inexistente responde error', typeof res.error === 'string', res.error);
  },
};
