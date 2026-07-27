---
title: DB Query MCP
description: Referencia del MCP SQL de desarrollo consolidado en db_exec
---

## DB Query MCP

Servidor MCP local para SQL Server de **desarrollo** (EvistaDev). Usa
`.local-secrets/sqlserver.json` sin devolver valores sensibles. Para producción usar
`db-query-pro`.

Este directorio es el paquete base portable: contiene protocolo, ejecución SQL,
reportes, discovery de tools y smoke bajo `src/`. Puede copiarse completo a otro
proyecto; solo requiere crear `.local-secrets/sqlserver.json` en la raíz del proyecto
destino. Este perfil fija la identidad del servidor, el archivo local de secretos y
la política de desarrollo.

## Tool

| Tool | Uso |
|---|---|
| `db_exec` | Ejecuta bloques T-SQL ordenados y reemplaza un reporte Markdown. |

`db_exec` es la única tool expuesta. Cada módulo en `tools/` exporta únicamente
`definition` y `execute`. Cada resultado se guarda bajo `.mcp/db-query/reports/`
sin exponer valores sensibles.

## Ejecución

```bash
npm --prefix .mcp/db-query install
node .mcp/db-query/server.mjs --project-root .
node .mcp/db-query/tests/smoke.mjs
```

Nunca colocar secretos en `.vscode/mcp.json`, `.mcp.json`, prompts, logs ni reportes.
