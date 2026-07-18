# DB Query MCP — Bancos

MCP local de solo lectura para consultar `dbbancos` sin exponer credenciales. Lee configuración desde `.local-secrets/db.json`; nunca retorna servidor, usuario, contraseña ni base configurada.

## Tools

| Tool | Uso |
|---|---|
| `db_status` | Verifica configuración y conexión sin revelar secretos. |
| `db_list_tables` | Lista tablas y vistas accesibles. |
| `db_describe_table` | Describe columnas de una tabla. |
| `db_query` | Ejecuta una única consulta `SELECT`/CTE con máximo, timeout y saneamiento de campos sensibles. |

Todas las consultas son de solo lectura. DDL, DML, procedimientos, múltiples sentencias, comentarios SQL y columnas sensibles son rechazados. El resultado llega saneado al agente y se guarda localmente bajo `.local-output/db-query/` con permisos restringidos para auditoría.

## Ejecución y validación

```bash
node .mcp/db-query/tests/smoke.mjs
```

Configuraciones de cliente: `.vscode/mcp.json`, `.mcp.json` y ejemplos. Codex requiere abrir una sesión nueva después de actualizar `~/.codex/config.toml`.
