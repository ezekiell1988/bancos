import { CLASES, ToolError, leerTipos, tiposFile, writeJsonl } from '../src/datos.mjs';
import { refrescarSqlite } from '../src/sqlite.mjs';

export default {
  name: 'bancos_renombrar_tipo',
  order: 12,
  description:
    'Cambia el nombre visible de un tipo de gasto o de ingreso (el id no cambia, así los movimientos siguen igual). ' +
    'Sin apply:true solo muestra el preview.',
  inputSchema: {
    type: 'object',
    properties: {
      clase: { type: 'string', enum: CLASES },
      id: { type: 'string', description: 'id del tipo (bancos_tipos).' },
      nombre: { type: 'string', description: 'Nombre visible nuevo.' },
      apply: { type: 'boolean', description: 'true para guardarlo; sin él solo preview.' },
    },
    required: ['clase', 'id', 'nombre'],
    additionalProperties: false,
  },
  async handler({ clase, id, nombre, apply }) {
    const tipos = leerTipos(clase);
    const tipo = tipos.find((t) => t.id === id);
    if (!tipo) throw new ToolError(`No existe el tipo de ${clase} "${id}".`);
    if (!nombre.trim()) throw new ToolError('nombre vacío');
    const antes = tipo.nombre;
    if (apply !== true) return { preview: true, applied: false, clase, id, antes, despues: nombre.trim(), message: 'Llamar de nuevo con apply:true para guardarlo.' };
    tipo.nombre = nombre.trim();
    writeJsonl(tiposFile(clase), tipos);
    return { aviso: refrescarSqlite(), applied: true, clase, id, antes, despues: tipo.nombre };
  },
  async smoke({ callTool, check, toolJson, toolText }) {
    const prev = toolJson(await callTool('bancos_renombrar_tipo', { clase: 'gasto', id: 'mascotas', nombre: 'Mascotas y veterinaria' }));
    check('bancos_renombrar_tipo hace preview sin apply', prev.preview === true && prev.antes === 'Mascotas', JSON.stringify(prev));
    const ok = toolJson(await callTool('bancos_renombrar_tipo', { clase: 'gasto', id: 'mascotas', nombre: 'Mascotas y veterinaria', apply: true }));
    const lista = toolText(await callTool('bancos_tipos', { clase: 'gasto' }));
    check('bancos_renombrar_tipo guarda con apply', ok.applied === true && lista.includes('Mascotas y veterinaria'), lista.slice(0, 200));
    const no = toolJson(await callTool('bancos_renombrar_tipo', { clase: 'gasto', id: 'nada', nombre: 'X' }));
    check('bancos_renombrar_tipo rechaza id inexistente', /No existe/.test(no.error ?? ''), no.error);
  },
};
