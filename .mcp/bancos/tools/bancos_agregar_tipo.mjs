import { CLASES, ToolError, appendJsonl, leerTipos, slug, tiposFile } from '../src/datos.mjs';
import { refrescarSqlite } from '../src/sqlite.mjs';

export default {
  name: 'bancos_agregar_tipo',
  order: 11,
  description:
    'Agrega un tipo nuevo de gasto o de ingreso a datos/tipos-*.jsonl. Sin apply:true solo muestra el preview. ' +
    'Usar solo si ningún tipo de bancos_tipos sirve.',
  inputSchema: {
    type: 'object',
    properties: {
      clase: { type: 'string', enum: CLASES, description: 'gasto o ingreso.' },
      nombre: { type: 'string', description: 'Nombre visible, p. ej. "Mascotas".' },
      id: { type: 'string', description: 'Id en minúsculas con guiones. Default: derivado del nombre.' },
      apply: { type: 'boolean', description: 'true para guardarlo; sin él solo preview.' },
    },
    required: ['clase', 'nombre'],
    additionalProperties: false,
  },
  async handler({ clase, nombre, id, apply }) {
    const tipo = { id: slug(id || nombre), nombre: nombre.trim() };
    if (!tipo.id || !tipo.nombre) throw new ToolError('nombre vacío');
    const existente = leerTipos(clase).find((t) => t.id === tipo.id);
    if (existente) throw new ToolError(`Ya existe el tipo de ${clase} "${tipo.id}" (${existente.nombre}).`);
    if (apply !== true) return { preview: true, applied: false, clase, tipo, message: 'Llamar de nuevo con apply:true para guardarlo.' };
    appendJsonl(tiposFile(clase), [tipo]);
    return { aviso: refrescarSqlite(), applied: true, clase, tipo };
  },
  async smoke({ callTool, check, toolJson }) {
    const prev = toolJson(await callTool('bancos_agregar_tipo', { clase: 'gasto', nombre: 'Mascotas' }));
    check('bancos_agregar_tipo hace preview sin apply', prev.preview === true && prev.tipo?.id === 'mascotas', JSON.stringify(prev));
    const ok = toolJson(await callTool('bancos_agregar_tipo', { clase: 'gasto', nombre: 'Mascotas', apply: true }));
    check('bancos_agregar_tipo guarda con apply', ok.applied === true, JSON.stringify(ok));
    const dup = toolJson(await callTool('bancos_agregar_tipo', { clase: 'gasto', nombre: 'Mascotas', apply: true }));
    check('bancos_agregar_tipo rechaza duplicado', typeof dup.error === 'string', dup.error);
  },
};
