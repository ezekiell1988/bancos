# MCP azureAi — Azure AI Foundry v1 (chat, imágenes, video, Cleo)

MCP local que expone 7 tools para operar Azure AI Foundry v1 (ITQS):
`azure_ai_chat` (razonamiento con gpt-5.5), `azure_ai_generate_image` / `azure_ai_edit_image`
(gpt-image-2), `azure_ai_generate_video` (sora-2), `azure_ai_generate_cleo_poses` (poses de
la mascota Cleo preservando identidad), `azure_ai_generate_transparent_image` (chroma key
verde + recorte con el script Python existente) y `azure_ai_status` (diagnóstico de
credenciales/modelos). Ver skill `azure-ai` para el detalle de uso de cada tool.

## Arquitectura

Node.js (`stdio`, JSON-RPC newline-delimited) siguiendo el skill `mcp-vscode` (ADR-123): 1
tool = 1 archivo en `tools/`, autodescubierto por el runtime genérico compartido
`.mcp/_shared/`. Es el único servidor MCP local con un subproceso externo
(`scripts/green_screen_cutout.py`, invocado vía `node:child_process`).

```text
.mcp/azure-ai/
├── server.mjs               ← adaptador delgado sobre _shared/runtime.mjs. No se edita al agregar tools
├── src/
│   ├── common.mjs           ← initProjectRoot()/getProjectRoot()
│   ├── credentials.mjs      ← loadCredentials(): lee ai-foundry.json (con caché)
│   ├── client.mjs           ← cliente REST vía fetch nativo: chat, generateImage, editImage,
│   │                           createVideoJob/pollVideoJob/downloadVideo, checkEndpoint
│   └── workspace-paths.mjs  ← resolveWorkspacePath(): confina outputPath/imagePath al workspace
├── tools/
│   ├── azure_ai_chat.mjs
│   ├── azure_ai_generate_image.mjs
│   ├── azure_ai_edit_image.mjs
│   ├── azure_ai_generate_video.mjs
│   ├── azure_ai_generate_cleo_poses.mjs
│   ├── azure_ai_generate_transparent_image.mjs   ← spawnea python3 scripts/green_screen_cutout.py
│   └── azure_ai_status.mjs
├── scripts/
│   └── green_screen_cutout.py   ← recorte chroma-key determinístico (no ML), sin cambios
└── tests/
    └── smoke.mjs             ← arnés de _shared/test-harness.mjs; no hace llamadas reales con costo
```

Las llamadas HTTP a Azure AI Foundry usan `fetch` nativo de Node (sin dependencias npm).

## Credenciales

Se leen desde `.local-secrets/ai-foundry.json`, `credentials/ai-foundry.json` o
`src/demo/credentials/ai-foundry.json` (el primero que exista), sin exponerlas nunca al
modelo. `azure_ai_status` reporta qué archivo se usó (`credentialsFile`) y enmascara la
`apiKey`.

## Catálogo de tools

| Tool | Propósito | Parámetros clave |
|---|---|---|
| `azure_ai_chat` | Chat completion con gpt-5.5; opcionalmente guarda la respuesta en `outputPath` | `prompt` (req), `systemPrompt`, `model`, `maxCompletionTokens`, `outputPath` |
| `azure_ai_generate_image` | Genera imagen con gpt-image-2 | `prompt` (req), `outputPath` (req), `size`, `quality`, `outputFormat` |
| `azure_ai_edit_image` | Edita una imagen del workspace con gpt-image-2 (multipart) | `imagePath` (req), `prompt` (req), `outputPath` (req), `maskPath`, `size`, `quality`, `inputFidelity` |
| `azure_ai_generate_video` | Genera video con sora-2, hace polling y descarga el MP4 en `outputPath` | `prompt` (req), `outputPath` (req), `seconds`, `size`, `maxWaitSeconds`, `pollIntervalSeconds` |
| `azure_ai_generate_cleo_poses` | Genera poses de Cleo preservando identidad desde una referencia, con backup opcional | `poses` (array/`["all"]`), `framing`, `outputDir`, `referenceImagePath`, `backup` |
| `azure_ai_generate_transparent_image` | Genera sobre chroma key verde y ejecuta `scripts/green_screen_cutout.py` para producir PNG con alfa | `prompt` (req), `outputPath` (req), `referenceImagePath`, `keepGreenScreenCopy` |
| `azure_ai_status` | Diagnóstico de credenciales, endpoint y modelos configurados | `checkEndpoint` (bool) |

Todas las rutas (`outputPath`/`imagePath`/`referenceImagePath`) se resuelven contra
`--project-root` vía `src/workspace-paths.mjs#resolveWorkspacePath`, que acepta rutas
relativas o absolutas dentro del workspace y rechaza cualquier traversal (`..`) fuera de esa
raíz.

## Política de escritura

Los tools de generación escriben archivos (texto/PNG/MP4) directamente en `outputPath`/
`outputDir` dentro del workspace — no hay preview/`apply` porque no mutan estado del proyecto
(`ia/`, entidades, config), solo generan artefactos de salida pedidos explícitamente por el
parámetro. `azure_ai_status` es de solo lectura.

## Ejecución local

```bash
node .mcp/azure-ai/server.mjs --project-root /ruta/al/proyecto
```

| Flag | Default | Uso |
|---|---|---|
| `--project-root` | `process.cwd()` | Raíz del proyecto; ahí se resuelven credenciales, `outputPath`, `imagePath` y el script Python. |

## Configuración por cliente

### VS Code (`.vscode/mcp.json`)

```json
"azureAi": {
  "type": "stdio",
  "command": "node",
  "args": ["${workspaceFolder}/.mcp/azure-ai/server.mjs", "--project-root", "${workspaceFolder}"],
  "cwd": "${workspaceFolder}",
  "dev": { "watch": [".mcp/_shared/**/*.mjs", ".mcp/azure-ai/**/*.mjs"] }
}
```

### Claude Code (`.mcp.json`)

```json
{
  "mcpServers": {
    "azureAi": {
      "command": "node",
      "args": [".mcp/azure-ai/server.mjs", "--project-root", "."]
    }
  }
}
```

### Codex (`~/.codex/config.toml`)

```toml
[mcp_servers.azure_ai]
command = "node"
cwd = "/ruta/al/proyecto"
args = [".mcp/azure-ai/server.mjs", "--project-root", "."]
```

## Validación

```bash
node --check .mcp/_shared/*.mjs .mcp/azure-ai/server.mjs \
  .mcp/azure-ai/src/*.mjs .mcp/azure-ai/tools/*.mjs
node .mcp/azure-ai/tests/smoke.mjs
```

El smoke valida: catálogo con exactamente las 7 tools esperadas, negociación de
`protocolVersion` (no echo ciego), rechazo de parámetros requeridos faltantes
(`azure_ai_chat` sin `prompt`), rechazo de rutas fuera del workspace antes de llamar a la API
(`azure_ai_generate_image`/`azure_ai_generate_video`/`azure_ai_generate_transparent_image`),
rechazo de referencia inexistente (`azure_ai_edit_image`, `azure_ai_generate_cleo_poses`) y
`azure_ai_status` contra las credenciales reales del repo (`status:"ready"`, `apiKeyMasked`
enmascarada) — no hace llamadas de generación reales (sin costo). Paridad verificada
manualmente con una generación real: `azure_ai_generate_image` produjo un PNG 1024×1024
válido contra el endpoint real de Azure AI Foundry.

## Protocolo

- `stdio` con JSON-RPC delimitado por saltos de línea (`\n`), administrado por
  VS Code/Claude Code/Codex como proceso hijo. Nunca `Content-Length` framing.
- Logs solo a `stderr` (`.mcp/_shared/results.mjs#log`); `stdout` es exclusivo del
  protocolo MCP.

Documentación de conceptos MCP: <https://modelcontextprotocol.io/> y <https://code.visualstudio.com/docs/copilot/chat/mcp-servers>.

## Código .NET anterior

`src/AzureAi.Tools`, `src/AzureAi.Stdio` y `tests/AzureAi.StdioSmoke` se mantienen intactos
por ahora (fuera de alcance de TASK-EBC-MCP-95 borrarlos) — se retiran en una tarea de
cierre posterior una vez validado el equivalente Node en producción, igual que se hizo con
`googleAi` (ADR-119/ADR-123).
