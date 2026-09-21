import { getWorkflow } from '../src/common.mjs';

export default {
  name: 'resolve_phase_review',
  description: 'Resuelve exactamente un indicador de revisión arquitectónica o retrospectiva de fase con evidencia. La vista previa es predeterminada.',
  inputSchema: {
    type: 'object',
    properties: {
      phase: { type: 'string', description: 'Phase heading.' },
      review: { type: 'string', description: 'architecture or retrospective.' },
      outcome: { type: 'string', description: 'updated, recorded or not_applicable.' },
      evidence: { type: 'string', description: 'Evidence for the result.' },
      references: { type: 'array', items: { type: 'string' }, description: 'Required when architecture is updated.' },
      apply: { type: 'boolean', description: 'Set true to apply.' },
    },
    required: ['phase', 'review', 'outcome', 'evidence'],
    additionalProperties: false,
  },
  async handler(args) {
    return getWorkflow().resolvePhaseReview(args.phase, args.review, args.outcome, args.evidence, args.references, args.apply === true);
  },
  async smoke({ callTool, check, toolJson }) {
    const res = toolJson(
      await callTool('resolve_phase_review', { phase: 'Fase inexistente smoke', review: 'architecture', outcome: 'not_applicable', evidence: 'smoke' }),
    );
    check('resolve_phase_review con fase inexistente responde error', typeof res.error === 'string', res.error);
  },
};
