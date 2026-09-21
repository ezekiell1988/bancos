import { getWorkflow } from '../src/common.mjs';

export default {
  name: 'ia_create_decision',
  description: 'Crea un ADR estructurado y actualiza el índice de decisiones. La vista previa es predeterminada.',
  inputSchema: {
    type: 'object',
    properties: {
      title: { type: 'string', description: 'ADR title.' },
      domain: { type: 'string', description: 'Decision domain.' },
      context: { type: 'string', description: 'Context.' },
      decision: { type: 'string', description: 'Decision.' },
      reason: { type: 'string', description: 'Reason.' },
      status: { type: 'string', description: 'propuesta, aceptada or reemplazada.' },
      alternatives: { type: 'array', items: { type: 'string' }, description: 'Alternatives considered.' },
      consequences: { type: 'array', items: { type: 'string' }, description: 'Consequences.' },
      replaces: { type: 'string', description: 'Optional ADR replaced.' },
      apply: { type: 'boolean', description: 'Set true to apply.' },
    },
    required: ['title', 'domain', 'context', 'decision', 'reason'],
    additionalProperties: false,
  },
  async handler(args) {
    return getWorkflow().createDecision(
      args.title, args.domain, args.context, args.decision, args.reason, args.status,
      args.alternatives, args.consequences, args.replaces, args.apply === true,
    );
  },
  async smoke({ callTool, check, toolJson }) {
    const res = toolJson(
      await callTool('ia_create_decision', {
        title: 'Smoke ADR',
        domain: 'MCP',
        context: 'ctx',
        decision: 'dec',
        reason: 'smoke',
      }),
    );
    check('ia_create_decision en preview no escribe', res.preview === true && res.applied === false, JSON.stringify(res).slice(0, 150));
  },
};
