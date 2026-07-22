# 02 — Estructura del servidor: carpeta `tools/` y autodescubrimiento

Arquitectura optimizada para que un LLM agregue y edite tools con la mínima fricción:
todo lo que define un tool (schema, handler, formato, smoke test) vive en **un solo
archivo** que el server descubre automáticamente.

## Estructura de carpetas

```text
.mcp/
  {nombre-servidor}/
    server.mjs            ← arranque genérico. NO se edita al agregar tools
    README.md             ← qué hace el server, cómo correr el smoke, decisiones
    tools/                ← ★ 1 archivo = 1 tool, autodescubierto
      devops_login.mjs
      devops_list_projects.mjs
      devops_create_work_item.mjs
      _helpers.mjs        ← prefijo "_" = ignorado por el registry (helpers compartidos)
    src/                  ← infraestructura compartida entre tools
      registry.mjs        ← autodescubrimiento + validación de contrato
      protocol.mjs        ← McpServer: initialize, tools/list, tools/call, framing
      common.mjs          ← ToolError, textResult, toonResult, errorResult, log
      toon.mjs            ← encoder TOON para listados
      {dominio}.mjs       ← módulos de dominio (auth.mjs, az.mjs, fs.mjs, markdown.mjs...)
    tests/
      smoke.mjs           ← runner genérico: deriva el catálogo de tools/ y ejecuta
                            el smoke() de cada tool. OBLIGATORIO antes de dar por listo
    examples/
      vscode-mcp.json     ← config reutilizable para otros proyectos
      codex-config.toml
```

Usar otra ubicación solo si hay razón fuerte documentada.

## Contrato del archivo de tool

Cada `tools/{name}.mjs` exporta **default** un objeto con este shape
(plantilla completa en [../examples/tool-template.mjs](../examples/tool-template.mjs)):

```js
export default {
  name: 'devops_list_projects',   // OBLIGATORIO: igual al nombre del archivo sin .mjs
  description: '...',             // OBLIGATORIO: el modelo la usa para decidir cuándo llamar
  inputSchema: {                  // OBLIGATORIO: JSON Schema con additionalProperties:false
    type: 'object',
    properties: { /* ... */ },
    required: [],
    additionalProperties: false,
  },
  async handler(args) { /* ... */ },  // OBLIGATORIO: retorna el payload (objeto o array)

  format: 'toon',                 // opcional: 'toon' (listados) | 'json' (default)
  order: 0,                       // opcional: menor = primero en tools/list (default 100).
                                  //           usar 0 solo para el tool de login/init
  async smoke(ctx) { /* ... */ }, // opcional pero recomendado: checks co-ubicados
};
```

Reglas que el registry valida al cargar (falla rápido nombrando el archivo):

- `name` = nombre del archivo sin `.mjs`, en `snake_case`.
- `description` no vacía.
- `inputSchema.type === 'object'` y `additionalProperties === false`.
- `handler` es función; `smoke` (si existe) es función.
- `format` (si existe) es `'toon'` o `'json'`.

## El registry (`src/registry.mjs`)

Implementación de referencia en [../examples/registry.mjs](../examples/registry.mjs).
Comportamiento:

1. Lista `tools/*.mjs` ignorando archivos con prefijo `_`.
2. Importa cada archivo dinámicamente (`await import(pathToFileURL(...))`).
3. Valida el contrato; un archivo inválido detiene el arranque con
   `tools/{archivo}: {motivo}` — el error llega a stderr y VS Code lo muestra en Output.
4. Ordena por `order` (default 100) y luego alfabético → ese es el orden de `tools/list`.

## `server.mjs` genérico

Con el registry, el server queda reducido a arranque + dispatch y **nunca cambia al
agregar tools** (esqueleto completo en
[../examples/server-skeleton.mjs](../examples/server-skeleton.mjs)):

```js
const tools = await loadTools(path.join(here, 'tools'));
const byName = new Map(tools.map((t) => [t.name, t]));

async function callTool(name, args) {
  const tool = byName.get(name);
  if (!tool) return errorResult(`tool desconocida: ${name}`);
  try {
    const payload = await tool.handler(args ?? {});
    return tool.format === 'toon' ? toonResult(payload) : textResult(payload);
  } catch (err) {
    if (err instanceof ToolError) return errorResult(err.message);
    log(`error inesperado en ${name}: ${err.stack}`);
    return errorResult(`error interno: ${err.message}`);
  }
}
```

## Migración desde la estructura antigua

Estructura antigua: `src/definitions.mjs` (array `TOOLS`) + `src/tools.mjs`
(implementaciones) + `HANDLERS` y `TOON_TOOLS` en `server.mjs`. Agregar un tool tocaba
4 archivos; la nueva estructura toca 1.

| Antes | Después |
|-------|---------|
| Entrada en `TOOLS` de `definitions.mjs` | `name/description/inputSchema` del archivo del tool |
| Función en `src/tools.mjs` | `handler` del archivo del tool |
| Entrada en `HANDLERS` de `server.mjs` | — (autodescubierto) |
| Entrada en `TOON_TOOLS` de `server.mjs` | `format: 'toon'` en el archivo del tool |
| Checks en `tests/smoke.mjs` | `smoke()` del archivo del tool |

Pasos de migración por tool: crear `tools/{name}.mjs`, mover el descriptor y la función,
declarar `format: 'toon'` si estaba en `TOON_TOOLS`, mover sus checks del smoke a
`smoke()`. Helpers compartidos entre tools van a `tools/_helpers.mjs` o a `src/{dominio}.mjs`.
Al terminar, `definitions.mjs` y los mapas de `server.mjs` desaparecen. Correr el smoke
después de migrar cada grupo de tools, no al final de todo.
