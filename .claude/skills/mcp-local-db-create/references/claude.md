---
title: Registrar DB Query en Claude Code
description: Configuracion de dbQuery para Claude Code mediante mcp.json
---

## Claude Code

Agregar la entrada `dbQuery` dentro de `mcpServers` en `.mcp.json`, en la raiz del
repositorio. Las rutas son relativas a esa raiz.

```json
{
  "mcpServers": {
    "dbQuery": {
      "command": "node",
      "args": [
        ".mcp/db-query/server.mjs",
        "--project-root",
        "."
      ],
      "env": {}
    }
  }
}
```

## Pasos

1. Ejecutar `examples/install-db-query.ps1` desde la raiz del proyecto.
2. Ejecutar `node .mcp/db-query/tests/smoke.mjs`.
3. Reiniciar la sesion de Claude Code despues de guardar `.mcp.json`.
4. Confirmar que aparece `mcp__dbQuery__db_exec`.

Mantener `env` vacio; las credenciales pertenecen a `.local-secrets/sqlserver.json`.