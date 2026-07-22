---
title: Registrar DB Query en VS Code
description: Configuracion de dbQuery para VS Code y GitHub Copilot
---

## VS Code / GitHub Copilot

Agregar la entrada `dbQuery` dentro de `servers` en `.vscode/mcp.json`. Conservar los
servidores existentes y no agregar secretos.

```json
{
  "servers": {
    "dbQuery": {
      "type": "stdio",
      "command": "node",
      "args": [
        "${workspaceFolder}/.mcp/db-query/server.mjs",
        "--project-root",
        "${workspaceFolder}"
      ],
      "dev": {
        "watch": ".mcp/db-query/**/*.mjs",
        "debug": {
          "type": "node"
        }
      }
    }
  }
}
```

## Pasos

1. Instalar dependencias con `examples/install-db-query.ps1`.
2. Ejecutar `node .mcp/db-query/tests/smoke.mjs`.
3. Abrir la vista MCP de VS Code y reiniciar `dbQuery`, o recargar la ventana.
4. Confirmar que aparece la tool `db_exec`.

`.vscode/mcp.json` puede versionarse. Las credenciales viven exclusivamente en
`.local-secrets/sqlserver.json` y ese archivo debe permanecer ignorado.