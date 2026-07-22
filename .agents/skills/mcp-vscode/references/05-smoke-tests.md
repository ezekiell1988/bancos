# 05 — Smoke tests: runner genérico + `smoke()` por tool

Ejecutar **antes** de considerar el servidor listo. Plantilla completa del runner en
[../examples/smoke-test.mjs](../examples/smoke-test.mjs).

## Arquitectura en dos capas

Con la estructura `tools/`, el smoke test se divide:

| Capa | Vive en | Qué verifica |
|------|---------|--------------|
| **Runner genérico** (`tests/smoke.mjs`) | Una vez por servidor, no crece con los tools | Handshake, negociación, catálogo completo, contrato del registry |
| **`smoke()` por tool** (`tools/{name}.mjs`) | Co-ubicado con el tool | Comportamiento concreto de ese tool |

El runner deriva el catálogo esperado **leyendo la carpeta `tools/`** (mismo criterio que
el registry: `*.mjs` sin prefijo `_`) — no hay lista `EXPECTED_TOOLS` que mantener a mano.
Luego importa cada archivo y ejecuta su `smoke(ctx)` si existe.

## Checks genéricos del runner

| Verificación | Qué confirma |
|---|---|
| `initialize` responde serverInfo | Server arranca y negocia |
| `protocolVersion` desconocida → newest soportada | No echo ciego |
| `tools/list` == archivos de `tools/` | Catálogo completo, sin tools fantasma ni faltantes |
| Tool con `order: 0` aparece primera (si existe) | Login/init visible primero |
| Argumento requerido faltante → `{ error }` claro | Validación de schema funciona |

## El contexto `ctx` que recibe cada `smoke()`

```js
{
  rpc,          // rpc(method, params, timeoutMs) → Promise<respuesta JSON-RPC>
  notify,       // notify(method, params) → notificación sin respuesta
  callTool,     // callTool(name, args) → atajo de rpc('tools/call', {name, arguments})
  check,        // check(nombre, condición, detalle) → registra OK/FAIL
  toolJson,     // extrae y parsea el payload JSON de una respuesta (tools sin format toon)
  toolText,     // extrae el texto crudo (tools TOON) — asserts por substring/regex
  state,        // objeto compartido entre smokes: p.ej. state.loggedIn que setea el
                //   smoke del tool de login; los demás hacen early-return si es false
}
```

Los `smoke()` corren en el orden del catálogo (por `order`, luego alfabético), así el
tool de login (order 0) puebla `state` antes que el resto.

## Reglas de aserción

- JSON-RPC delimitado por `\n` (newline-delimited), **nunca `Content-Length`**.
- Un `check()` por afirmación; nunca agrupar condiciones no relacionadas en un solo check.
- Tools TOON → `toolText()` + regex/substring, **no** `JSON.parse`.
- Tools JSON → `toolJson()` + acceso a campos concretos.
- Escrituras reales (create/delete) → `try/finally` para garantizar cleanup; títulos con
  marcador `[SMOKE TEST]` para identificar residuos.
- Mínimo por tool: responde + contiene un valor concreto conocido del entorno real.

| Tipo de tool | Helper | Qué verificar |
|---|---|---|
| Listado (TOON) | `toolText` | `/total: \d+/`, substring esperado, que NO empiece con `{` |
| Objeto (JSON) | `toolJson` | Campos específicos, tipos, valores |
| Error esperado | `toolJson` | `typeof r.error === 'string'` + keyword del mensaje |
| Escritura | `toolJson` | Sin `apply:true` → `preview === true && applied === false` |
| Paths | `toolJson` | `../` rechazado con error |

## Ejecución

```bash
# 1. Sintaxis de todo el server (incluye tools/)
node --check .mcp/mi-servidor/server.mjs
node --check .mcp/mi-servidor/tools/mi_tool_nueva.mjs

# 2. Smoke completo
node .mcp/mi-servidor/tests/smoke.mjs
# → "SMOKE TEST: TODO OK" y exit code 0, o lista de FAIL y exit code 1
```

El registry ya hace de validador estructural: si un archivo de tool viola el contrato
(name ≠ archivo, falta `additionalProperties: false`, handler no es función), el server
no arranca y el smoke falla en el `initialize` con el mensaje del registry en stderr.
