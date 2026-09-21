# MCP googleAi — Google Gemini API (texto y video Veo)

MCP local que expone 3 tools para operar la Google Gemini API
(`generativelanguage.googleapis.com/v1beta`): `google_ai_chat` (texto/razonamiento),
`google_ai_generate_video` (video Veo 3.1 / Veo 3.1 Fast, texto→video e imagen→video) y
`google_ai_status` (diagnóstico de credenciales/modelos). Ver skill `google-ai` para el
detalle de uso de cada tool.

## Arquitectura

Node.js (`stdio`, JSON-RPC newline-delimited) siguiendo el skill `mcp-vscode` (ADR-123,
revierte ADR-119): 1 tool = 1 archivo en `tools/`, autodescubierto por el runtime genérico
compartido `.mcp/_shared/`.

```text
.mcp/google-ai/
├── server.mjs               ← adaptador delgado sobre _shared/runtime.mjs. No se edita al agregar tools
├── src/
│   ├── common.mjs           ← initProjectRoot()/getProjectRoot()
│   ├── credentials.mjs      ← loadCredentials(): fusiona ai-google.json + ai-google-veo.json (con caché)
│   ├── client.mjs           ← cliente REST vía fetch nativo: generateContent, createVideoJob,
│   │                           pollVideoOperation, downloadVideoFromOperation, checkEndpoint
│   └── workspace-paths.mjs  ← resolveWorkspacePath(): confina outputPath/imagePath al workspace
├── tools/
│   ├── google_ai_chat.mjs
│   ├── google_ai_generate_video.mjs
│   └── google_ai_status.mjs
└── tests/
    └── smoke.mjs            ← arnés de _shared/test-harness.mjs; no hace llamadas reales con costo
```

Las llamadas HTTP a Gemini/Veo usan `fetch` nativo de Node (sin dependencias npm).

## Credenciales

Se leen desde `.local-secrets/` o `credentials/` (el primero que exista), sin exponerlas
nunca al modelo:

| Archivo | Contenido |
|---|---|
| `ai-google.json` | `apiKey`, `endpoint` (opcional), `defaultModel`, `textModels` |
| `ai-google-veo.json` | `apiKey`, `endpoint` (opcional), `models.video` / `models.videoFast` |

Ambos archivos comparten `apiKey`/`endpoint` en este proyecto pero se leen de forma
independiente — cualquiera de los dos puede faltar, no ambos. `google_ai_status` reporta
qué archivos se usaron (`credentialsFiles`) y enmascara la `apiKey`.

## Catálogo de tools

| Tool | Propósito | Parámetros clave |
|---|---|---|
| `google_ai_chat` | Genera texto con Gemini; opcionalmente guarda la respuesta en `outputPath` | `prompt` (req), `systemInstruction`, `model`, `temperature`, `maxOutputTokens`, `outputPath` |
| `google_ai_generate_video` | Genera video con Veo 3.1/3.1 Fast (texto→video o imagen→video vía `imagePath`), hace polling y descarga el MP4 en `outputPath` | `prompt` (req), `outputPath` (req), `imagePath`, `fast`, `aspectRatio`, `durationSeconds`, `resolution`, `maxWaitSeconds`, `pollIntervalSeconds` |
| `google_ai_status` | Diagnóstico de credenciales, endpoint y modelos configurados | `checkEndpoint` (bool) |

Todas las rutas (`outputPath`/`imagePath`) se resuelven contra `--project-root` vía
`src/workspace-paths.mjs#resolveWorkspacePath`, que acepta rutas relativas o absolutas
dentro del workspace y rechaza cualquier traversal (`..`) fuera de esa raíz.

## Política de escritura

`google_ai_chat` y `google_ai_generate_video` escriben archivos (texto/MP4) directamente
en `outputPath` dentro del workspace — no hay preview/`apply` porque no mutan estado del
proyecto (`ia/`, entidades, config), solo generan artefactos de salida pedidos
explícitamente por el parámetro. `google_ai_status` es de solo lectura.

## Ejecución local

```bash
node .mcp/google-ai/server.mjs --project-root /ruta/al/proyecto
```

| Flag | Default | Uso |
|---|---|---|
| `--project-root` | `process.cwd()` | Raíz del proyecto; ahí se resuelven credenciales, `outputPath` e `imagePath`. |

## Configuración por cliente

### VS Code (`.vscode/mcp.json`)

```json
"googleAi": {
  "type": "stdio",
  "command": "node",
  "args": ["${workspaceFolder}/.mcp/google-ai/server.mjs", "--project-root", "${workspaceFolder}"],
  "cwd": "${workspaceFolder}",
  "dev": { "watch": [".mcp/_shared/**/*.mjs", ".mcp/google-ai/**/*.mjs"] }
}
```

### Claude Code (`.mcp.json`)

```json
{
  "mcpServers": {
    "googleAi": {
      "command": "node",
      "args": [".mcp/google-ai/server.mjs", "--project-root", "."]
    }
  }
}
```

### Codex (`~/.codex/config.toml`)

```toml
[mcp_servers.google_ai]
command = "node"
cwd = "/ruta/al/proyecto"
args = [".mcp/google-ai/server.mjs", "--project-root", "."]
```

## Validación

```bash
node --check .mcp/_shared/*.mjs .mcp/google-ai/server.mjs \
  .mcp/google-ai/src/*.mjs .mcp/google-ai/tools/*.mjs
node .mcp/google-ai/tests/smoke.mjs
```

El smoke valida: catálogo con exactamente las 3 tools esperadas, negociación de
`protocolVersion` (no echo ciego), rechazo de parámetros requeridos faltantes
(`google_ai_chat` sin `prompt`), rechazo de `outputPath` fuera del workspace antes de
llamar a la API (`google_ai_generate_video`), y `google_ai_status` contra las credenciales
reales del repo si existen (`status:"ready"`, `apiKeyMasked` enmascarada) — no hace
llamadas de generación reales (sin costo). Paridad verificada manualmente: `google_ai_status`
produce una salida byte a byte idéntica a la versión .NET (mismo `apiKeyMasked`,
`credentialsFiles`, `models`).

## Protocolo

- `stdio` con JSON-RPC delimitado por saltos de línea (`\n`), administrado por
  VS Code/Claude Code/Codex como proceso hijo. Nunca `Content-Length` framing.
- Logs solo a `stderr` (`.mcp/_shared/results.mjs#log`); `stdout` es exclusivo del
  protocolo MCP.

Documentación de conceptos MCP: <https://modelcontextprotocol.io/> y <https://code.visualstudio.com/docs/copilot/chat/mcp-servers>.

## Código .NET anterior (ADR-119)

`src/GoogleAi.Tools`, `src/GoogleAi.Stdio`, `tests/GoogleAi.StdioSmoke`, `GoogleAi.sln`,
`Directory.Build.props` y el `.gitignore` de .NET se eliminaron tras validar el equivalente
Node (smoke test en verde y `google_ai_status` comparado byte a byte contra la versión .NET
aún conectada) — recuperables en el historial de git si hiciera falta.
