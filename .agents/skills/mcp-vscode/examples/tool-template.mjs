// Plantilla de tool MCP — copiar a .mcp/{servidor}/tools/{nombre_del_tool}.mjs y adaptar.
//
// REGLAS (el registry las valida al arrancar):
//   - `name` DEBE ser idéntico al nombre del archivo sin .mjs (snake_case).
//   - `inputSchema` DEBE tener additionalProperties: false.
//   - Errores de negocio → lanzar ToolError, nunca Error nativo.
//   - No hay que registrar nada más: el registry autodescubre este archivo.
//
// Después de completar:
//   node --check tools/{nombre_del_tool}.mjs
//   node tests/smoke.mjs

import { ToolError } from '../src/common.mjs';
// Helpers de dominio compartidos: ../src/{dominio}.mjs o ./_helpers.mjs

export default {
  // Igual al nombre del archivo. Descriptivo del dominio: 'devops_list_iterations' > 'list'.
  name: 'mi_tool_nueva',

  // 'toon' → arrays/listados uniformes (~40% menos tokens).
  // Omitir (JSON) → objetos de estado chicos o campos con HTML/texto largo.
  format: 'toon',

  // Opcional: menor = aparece primero en tools/list (default 100).
  // Usar 0 SOLO para el tool de login/init del servidor.
  // order: 100,

  // El modelo lee esto para decidir cuándo llamar el tool. Decir qué devuelve,
  // el formato (TOON/JSON) y cuándo NO usarlo si hay ambigüedad con otro tool.
  description:
    'Lista los foo del proyecto con id, nombre y estado. Respuesta en formato TOON. ' +
    'No usar para obtener el detalle completo de un foo (usar mi_tool_get).',

  inputSchema: {
    type: 'object',
    properties: {
      project: {
        type: 'string',
        description: 'Nombre del proyecto. Default: valor de config si existe.',
      },
      mode: {
        type: 'string',
        enum: ['full', 'summary'],
        description: 'Nivel de detalle. Default: summary.',
      },
      // Para write tools: preview por defecto, mutar solo con apply:true.
      // apply: { type: 'boolean', description: 'true para ejecutar; sin él solo preview.' },
    },
    required: ['project'],
    additionalProperties: false,
  },

  async handler(args) {
    if (!args.project) throw new ToolError('project requerido');

    // --- Lógica del tool: CLI / REST / filesystem (ver references/03-agregar-tool.md) ---
    const items = [];

    // Para write tools (safe write):
    // if (args.apply !== true) {
    //   return { preview: true, applied: false, message: 'Llama de nuevo con apply:true.', willCreate: {...} };
    // }

    // Mapear solo los campos que el modelo necesita — nunca volcar la respuesta cruda.
    return { project: args.project, total: items.length, items };
  },

  // Checks co-ubicados que ejecuta tests/smoke.mjs contra el server real por stdio.
  // Mínimo: que responde + un valor concreto conocido del entorno.
  async smoke({ callTool, check, toolText, toolJson, state }) {
    // Si el tool requiere sesión y el smoke del login la marcó ausente, saltar:
    // if (!state.loggedIn) return;

    const text = toolText(await callTool('mi_tool_nueva', { project: 'MI-PROYECTO' }));
    check('mi_tool_nueva devuelve items', /total: \d+/.test(text), text.split('\n')[0]);
    check('mi_tool_nueva es TOON (no JSON)', !text.trim().startsWith('{'), text.slice(0, 40));
    // check('mi_tool_nueva incluye "Foo esperado"', text.includes('Foo esperado'), text.slice(0, 200));

    const sinProject = toolJson(await callTool('mi_tool_nueva', {}));
    check(
      'mi_tool_nueva requiere project',
      typeof sinProject.error === 'string' && sinProject.error.includes('project'),
      sinProject.error,
    );
  },
};
