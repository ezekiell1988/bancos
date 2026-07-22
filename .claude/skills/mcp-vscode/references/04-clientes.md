# 04 — Configuración por cliente

El mismo server `stdio` sirve a los tres clientes; solo cambia dónde se declara.

## VS Code / GitHub Copilot — `.vscode/mcp.json`

```json
{
  "servers": {
    "miServidor": {
      "type": "stdio",
      "command": "node",
      "args": [
        "${workspaceFolder}/.mcp/mi-servidor/server.mjs",
        "--project-root",
        "${workspaceFolder}"
      ],
      "dev": {
        "watch": ".mcp/mi-servidor/**/*.mjs",
        "debug": { "type": "node" }
      }
    }
  }
}
```

- `dev.watch`: reinicia el servidor al cambiar las fuentes (sin reabrir VS Code). Con la
  estructura `tools/`, crear un archivo nuevo también dispara el reinicio → el tool
  aparece solo.
- `dev.debug`: VS Code adjunta un debugger Node al proceso.
- **No poner secretos aquí**. Si el servidor necesita credenciales, usar `inputs`:

```json
"inputs": [
  { "id": "myToken", "type": "promptString", "description": "API token", "password": true }
]
```

### Gitignore — excepción obligatoria

`.vscode/mcp.json` debe versionarse para que el equipo comparta la configuración.
Cuando `.vscode/` está en `.gitignore`:

```gitignore
.vscode/*
!.vscode/mcp.json
```

Git no puede re-incluir archivos dentro de un directorio excluido con `!.vscode/mcp.json`
solo — necesita el glob `*` más la negación.

> **Nota Windows**: `sandboxEnabled` de VS Code es macOS/Linux only. En Windows, el propio
> servidor debe confinar las lecturas/escrituras (p.ej. solo `.md` bajo el root del proyecto).

## Claude Code — `.mcp.json` en la raíz del repo

```json
{
  "mcpServers": {
    "miServidor": {
      "command": "node",
      "args": [
        ".mcp/mi-servidor/server.mjs",
        "--project-root",
        "."
      ],
      "env": {}
    }
  }
}
```

- Rutas relativas al root del repo (Claude Code lanza el proceso desde ahí).
- Cambios en `.mcp.json` requieren reiniciar la sesión de Claude Code.
- Las tools aparecen como `mcp__{servidor}__{tool}`.

## Codex — `~/.codex/config.toml`

Codex carga los servidores MCP al iniciar la sesión. Editar y abrir sesión nueva.

```toml
[mcp_servers.mi_servidor]
command = "node"
args = [
  "/ruta/al/proyecto/.mcp/mi-servidor/server.mjs",
  "--project-root",
  "/ruta/al/proyecto"
]
startup_timeout_sec = 30
```

Si el MCP está en config pero no aparece como tool nativa en la sesión actual: no simular
la llamada. Pedir al usuario que abra una nueva sesión o ejecutar el smoke test por
`stdio` directamente.

## Verificación

Desde terminal (más confiable, no depende de sesión del cliente):

```bash
node --check .mcp/mi-servidor/server.mjs
node .mcp/mi-servidor/tests/smoke.mjs
```

Desde el cliente (Copilot Chat en Agent Mode, Claude Code o Codex), pedir al modelo:

```text
Llama [tool_de_listado] con project=[nombre_proyecto].
Confirma que devolvió items y el formato es correcto.
```

En los `examples/` del server mantener `vscode-mcp.json` y `codex-config.toml` con
**placeholders** (`/ruta/al/proyecto`), nunca rutas absolutas del desarrollador.
