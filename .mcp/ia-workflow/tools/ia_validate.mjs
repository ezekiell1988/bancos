import { getWorkflow } from '../src/common.mjs';

export default {
  name: 'ia_validate',
  order: 0,
  description:
    'Valida los archivos y directorios requeridos de /ia, los metadatos V2 de tareas, los límites de tamaño y la estructura segura.',
  inputSchema: { type: 'object', properties: {}, required: [], additionalProperties: false },
  async handler() {
    return getWorkflow().validate();
  },
  async smoke({ callTool, check, toolJson }) {
    const res = toolJson(await callTool('ia_validate', {}));
    check('ia_validate responde valid:boolean', typeof res.valid === 'boolean', JSON.stringify(res).slice(0, 150));
  },
};
