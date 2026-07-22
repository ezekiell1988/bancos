# 03 — Agregar un tool: el flujo de 1 archivo

Agregar un tool a un MCP con estructura `tools/` toca **un solo archivo nuevo**.
No se edita `server.mjs`, ni `definitions.mjs` (no existe), ni el runner de smoke.

```text
1. Copiar examples/tool-template.mjs → tools/{nombre_del_tool}.mjs
2. Completar name, description, inputSchema, handler y smoke()
3. node --check tools/{nombre_del_tool}.mjs
4. node tests/smoke.mjs   → TODO OK
```

## Reglas del schema (`inputSchema`)

```js
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
  },
  required: ['project'],          // solo las verdaderamente requeridas
  additionalProperties: false,    // SIEMPRE: rechaza props inventadas por el modelo
},
```

- `name`: snake_case, descriptivo del dominio (`devops_list_iterations` > `list`).
- `description` del tool: el modelo la lee para decidir si llamar o no. Ser explícito
  sobre qué devuelve, el formato (TOON / JSON) y cuándo NO usarlo si hay ambigüedad.
- Defaults documentados en la `description` de cada prop; no usar JSON Schema `default`
  porque no todos los clientes lo respetan.
- Modos de lectura compactos para tools que devuelven contenido largo:
  `pathsOnly` (solo rutas/tamaños) · `summary` (heading + primer párrafo) · `full`
  (truncado a `maxChars`). Soportar `includeText: false` y `maxChars` para control de tokens.

## Reglas del `handler`

- Lanzar `ToolError` (no `Error` nativo) para errores de negocio — el server los devuelve
  al modelo como `{ error: "..." }` en lugar de crashear.
- Retornar solo los campos que el modelo necesita (mapear, no volcar la respuesta cruda).
- Validar paths para prevenir traversal (`../` → rechazar) y escanear secretos antes de
  escribir.
- Funciones puras cuando sea posible: más fáciles de testear con stubs.

## Patrón A — tool que usa un CLI (az, git, etc.)

```js
import { ready, runAzJson, readConfig } from '../src/az.mjs';

export default {
  name: 'devops_list_iterations',
  format: 'toon',
  description: 'Lista las iteraciones del proyecto. Respuesta en formato TOON.',
  inputSchema: { /* ... */ },
  async handler(args) {
    await ready(args);                                    // verifica sesión + configura org
    const project = args.project || readConfig().project; // fallback a config
    const result = await runAzJson(['boards', 'iteration', 'project', 'list', '--project', project]);
    const items = (result?.children ?? []).map((it) => ({
      id: it.id, name: it.name, startDate: it.attributes?.startDate ?? null,
    }));
    return { project, total: items.length, items };
  },
};
```

Nota az CLI: nunca pasar `--organization` explícito a `az devops`/`az boards` — rompe la
resolución de credenciales (usar `ensureOrgConfigured` + omitir el flag).

## Patrón B — tool que llama una REST API (PAT, API key)

```js
async handler(args) {
  const pat = readPat();                                 // credencial desde archivo local, nunca hardcodeada
  const b64 = Buffer.from(`:${pat}`).toString('base64');
  const res = await fetch(url, { headers: { Authorization: `Basic ${b64}` } });
  if (!res.ok) {
    const body = await res.text().catch(() => '');
    throw new ToolError(`Error ${res.status} al llamar la API.${body ? ' Detalle: ' + body.slice(0, 200) : ''}`);
  }
  const data = await res.json();
  return { total: data.value?.length ?? 0, items: (data.value ?? []).map((x) => ({ id: x.id, name: x.name })) };
}
```

## Patrón C — tool de escritura (safe write, obligatorio)

Toda tool que muta estado sigue este contrato:

1. Construir un plan de lo que va a pasar.
2. En **preview** (`apply !== true`): retornar el plan sin tocar nada.
3. En **apply** (`apply === true`): validar (secretos, paths, enums) → mutar.

```js
async handler(args) {
  if (!args.title) throw new ToolError('title requerido');

  if (args.apply !== true) {
    return {
      preview: true,
      applied: false,
      message: 'Llama de nuevo con apply:true para ejecutar.',
      willCreate: { title: args.title },
    };
  }

  const result = await runAzJson(['boards', 'work-item', 'create', '--title', args.title]);
  return { applied: true, id: result.id };
}
```

- **No exponer una tool genérica `write_file`** — el MCP expone workflows, no escritura
  arbitraria.
- Operaciones destructivas (delete) piden además `confirm: true`.

## Criterio `format: 'toon'` vs JSON (default)

TOON ahorra ~40% de tokens en respuestas tabulares. Decidir por el shape del payload:

| Resultado del handler | `format` |
|---|---|
| Array de items uniformes (iteraciones, proyectos, work items) | `'toon'` |
| Objeto de estado tras escritura (`{ applied: true, id: 42 }`) | JSON (omitir `format`) |
| Objeto con campos HTML/texto largo (descripción, acceptance criteria) | JSON (omitir `format`) |
| Config / defaults del servidor | `'toon'` |

## El `smoke()` co-ubicado

Cada tool declara sus verificaciones en el mismo archivo; el runner
(`tests/smoke.mjs`) las descubre y ejecuta contra el server real por stdio.
Mínimo dos checks: que responde + que contiene un valor concreto conocido.

```js
async smoke({ callTool, check, toolText, toolJson, state }) {
  if (!state.loggedIn) return;                           // saltar si no hay sesión

  const text = toolText(await callTool('devops_list_iterations', { project: 'ITQS-DEV-Team' }));
  check('devops_list_iterations devuelve items', /total: \d+/.test(text), text.split('\n')[0]);
  check('devops_list_iterations incluye "Iteration 3"', text.includes('Iteration 3'), text.slice(0, 200));
}
```

Detalle de helpers y reglas de aserción en [05-smoke-tests.md](05-smoke-tests.md).

## Checklist rápido al agregar un tool

- [ ] Archivo en `tools/{name}.mjs`; `name` interno idéntico al nombre del archivo
- [ ] `additionalProperties: false` en el schema
- [ ] `ToolError` (no `Error`) para errores de negocio
- [ ] `format: 'toon'` si devuelve un array de items
- [ ] Escritura → preview por defecto, muta solo con `apply: true`
- [ ] `smoke()` con al menos: responde + un valor concreto esperado
- [ ] `node --check tools/{name}.mjs` antes del smoke
- [ ] `node tests/smoke.mjs` → `TODO OK`
