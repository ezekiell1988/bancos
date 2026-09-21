import { getWorkflow } from '../src/common.mjs';

const strArray = (description) => ({ type: 'array', items: { type: 'string' }, description });

export default {
  name: 'create_task',
  description: 'Crea una tarea V2 completa en Borrador. La vista previa es predeterminada; apply=true escribe los archivos.',
  inputSchema: {
    type: 'object',
    properties: {
      title: { type: 'string', description: 'Short task title.' },
      area: { type: 'string', description: 'Task area.' },
      context: { type: 'string', description: 'Why this work is needed.' },
      objective: { type: 'string', description: 'Concrete objective.' },
      allowedScope: strArray('Allowed scope.'),
      outOfScope: strArray('Explicit exclusions.'),
      acceptanceCriteria: strArray('Acceptance criteria.'),
      technicalPlan: strArray('Technical plan.'),
      steps: strArray('Execution steps.'),
      expectedOutput: { type: 'string', description: 'Expected output.' },
      validation: strArray('Validation commands or evidence.'),
      rollback: { type: 'string', description: 'Safe rollback.' },
      priority: { type: 'string', description: 'baja, media, alta or critica.' },
      risk: { type: 'string', description: 'bajo, medio or alto.' },
      authorName: { type: 'string', description: 'Optional author name.' },
      authorEmail: { type: 'string', description: 'Optional author email.' },
      authorInitials: { type: 'string', description: 'Stable task initials.' },
      branch: { type: 'string', description: 'Optional branch.' },
      likelyFiles: strArray('Likely affected files.'),
      dependencies: strArray('Task dependencies.'),
      notes: { type: 'string', description: 'Optional notes.' },
      apply: { type: 'boolean', description: 'Set true to apply the planned workflow changes.' },
    },
    required: [
      'title', 'area', 'context', 'objective', 'allowedScope', 'outOfScope', 'acceptanceCriteria',
      'technicalPlan', 'steps', 'expectedOutput', 'validation', 'rollback',
    ],
    additionalProperties: false,
  },
  async handler(args) {
    return getWorkflow().createTask(args, args.apply === true);
  },
  async smoke({ callTool, check, toolJson }) {
    const res = toolJson(
      await callTool('create_task', {
        title: 'Smoke test create_task',
        area: 'MCP',
        context: 'ctx',
        objective: 'obj',
        allowedScope: ['x'],
        outOfScope: ['y'],
        acceptanceCriteria: ['z'],
        technicalPlan: ['plan'],
        steps: ['paso 1'],
        expectedOutput: 'output',
        validation: ['node --check'],
        rollback: 'revertir',
      }),
    );
    check('create_task en preview no escribe', res.preview === true && res.applied === false, JSON.stringify(res).slice(0, 150));
  },
};
