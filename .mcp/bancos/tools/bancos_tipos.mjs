import { CLASES, leerTipos } from '../src/datos.mjs';

export default {
  name: 'bancos_tipos',
  format: 'toon',
  order: 10,
  description:
    'Lista los tipos de gasto y de ingreso válidos (id y nombre) de datos/tipos-*.jsonl. ' +
    'Usar antes de bancos_registrar para elegir el tipo de cada movimiento. Respuesta TOON.',
  inputSchema: {
    type: 'object',
    properties: {
      clase: { type: 'string', enum: CLASES, description: 'Solo gasto o solo ingreso. Default: ambas.' },
    },
    additionalProperties: false,
  },
  async handler({ clase }) {
    const clases = clase ? [clase] : CLASES;
    return { tipos: clases.flatMap((c) => leerTipos(c).map((t) => ({ clase: c, id: t.id, nombre: t.nombre }))) };
  },
  async smoke({ callTool, check, toolText }) {
    const text = toolText(await callTool('bancos_tipos', {}));
    check('bancos_tipos lista gastos e ingresos', text.includes('comida') && text.includes('salario-itqs'), text.slice(0, 120));
  },
};
