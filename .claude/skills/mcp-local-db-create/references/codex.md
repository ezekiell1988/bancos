---
title: Registrar DB Query en Codex
description: Configuracion de dbQuery para Codex mediante config.toml
---

## Codex

Agregar este bloque a `~/.codex/config.toml` para uso personal, o a
`.codex/config.toml` si el repositorio ya comparte esa configuracion. Reemplazar
`C:/ruta/al/proyecto` con una ruta absoluta real y usar `/` en Windows.

```toml
[mcp_servers.db_query]
command = "node"
args = [
  "C:/ruta/al/proyecto/.mcp/db-query/server.mjs",
  "--project-root",
  "C:/ruta/al/proyecto"
]
enabled = true
startup_timeout_sec = 30
tool_timeout_sec = 90
default_tools_approval_mode = "prompt"
```

## Pasos

1. Ejecutar `examples/install-db-query.ps1` desde la raiz del proyecto.
2. Ejecutar `node .mcp/db-query/tests/smoke.mjs`.
3. Guardar `config.toml` y abrir una sesion nueva de Codex.
4. Confirmar que la tool aparece como `mcp_db_query_db_exec` o con el nombre mostrado
   por la sesion actual.

No anadir `Server`, `Database`, `User`, `Password` ni un archivo de secretos al TOML.