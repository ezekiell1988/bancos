# 01 — Protocolo: stdio, JSON-RPC y negociación

## Transporte

- **`stdio` con JSON-RPC delimitado por saltos de línea** (`\n`): un mensaje JSON-RPC por
  línea en `stdout`/`stdin`, sin newlines embebidos dentro del frame.
- **Nunca usar `Content-Length` framing** como protocolo `stdio` para VS Code. Si VS Code
  queda esperando `initialize` sin timeout, casi siempre es porque el servidor responde
  con `Content-Length` en lugar de newline-delimited.
- Logs solo a `stderr`; `stdout` debe contener únicamente mensajes MCP válidos. Cualquier
  `console.log` de debug rompe el stream.
- MCP no requiere Node, pero un `.mjs` sobre `stdio` es la opción más portable para
  VS Code, Claude Code y Codex a la vez.

## Flujo de mensajes

```text
cliente → initialize                    → servidor responde serverInfo + protocolVersion
cliente → notifications/initialized     → sin respuesta (es notificación, no tiene id)
cliente → tools/list                    → servidor responde { tools: [...] }
cliente → tools/call {name, arguments}  → servidor responde { content: [{type:'text', text}] }
```

- Los mensajes **sin `id`** son notificaciones: no se responden nunca.
- Todo resultado de tool tiene la forma `{ content: [{ type: 'text', text: '...' }] }`.
  Errores de negocio van como texto `{ "error": "..." }` dentro de `content`, no como
  error JSON-RPC — así el modelo puede leerlos y corregir la llamada.

## Negociación de `protocolVersion`

```js
const SUPPORTED_VERSIONS = ['2025-03-26', '2024-11-05'];

// En el handler initialize:
const requestedVersion = params.protocolVersion;
const version = SUPPORTED_VERSIONS.includes(requestedVersion)
  ? requestedVersion
  : SUPPORTED_VERSIONS[0]; // más reciente soportada
```

**Nunca hacer echo ciego** de `protocolVersion`. El cliente puede enviar una versión
futura que el servidor no implementa; echarla de vuelta hace que el cliente asuma
capacidades que no existen. El smoke test verifica este caso con una versión inventada
(`9999-99-99`).

## Primitivos MCP

VS Code soporta los tres:

| Primitivo | Superficie en VS Code | Cuándo usarlo |
|-----------|----------------------|---------------|
| `tools` | Agent Mode, Copilot Chat (invocación automática del modelo) | Acciones controladas por el modelo |
| `prompts` | Slash commands `/mcp.{server}.{prompt}` | Flujos repetibles, invocados por el usuario |
| `resources` | Botón "Add Context" | Contexto Markdown explícito que el usuario añade |

Para la mayoría de MCP locales de proyecto, `tools` es suficiente. Agregar `prompts` /
`resources` solo cuando haya un flujo de usuario concreto que los pida.

## Diseño del catálogo: workflow vocabulary vs building blocks

Exponer una **capa pública** de acciones de alto nivel (lo que el usuario dice en lenguaje
natural) y una **capa interna** de primitivas de bajo nivel (lo que el modelo usa
internamente). El usuario no debe necesitar saber nombres de tools internas.

```text
Usuario: "crea una tarea"   →   tool pública: create_task
                              └→ tool interna: ia_create_task (llamada dentro)
```
