---
title: Registrar MCP servers en Claude Code
description: Configuracion de MCPs stdio y SSE para Claude Code mediante .mcp.json
---

## Archivo de configuracion

Claude Code lee servidores MCP desde `.mcp.json` en la raiz del repositorio.
**No** desde `.claude/settings.json` ni `.claude/settings.local.json` — esos
archivos ignoran `mcpServers` silenciosamente.

## Tipos de transporte

### stdio (servidores locales con proceso hijo)

Para servidores que se ejecutan como proceso hijo de Claude Code.

```json
{
  "mcpServers": {
    "dbQuery": {
      "command": "node",
      "args": [".mcp/db-query/server.mjs", "--project-root", "."],
      "env": {}
    }
  }
}
```

### SSE (Server-Sent Events, servidores HTTP remotos o locales)

Para servidores que ya estan corriendo en un puerto. Requiere `"type": "sse"`.
Funciona con `http://` en localhost — no requiere HTTPS.

```json
{
  "mcpServers": {
    "bancos_mcp": {
      "type": "sse",
      "url": "http://localhost:8000/mcp/sse"
    }
  }
}
```

El endpoint SSE debe:
1. Responder GET con `Content-Type: text/event-stream`
2. Enviar un evento `event: endpoint` con `data: <url-para-POST-mensajes>`
3. Enviar respuestas JSON-RPC como eventos `event: message`

### HTTP (Streamable HTTP, recomendado para nuevos servidores)

SSE esta deprecated desde 2026. Para nuevos servidores usar `"type": "http"`
(alias `"streamable-http"` en la spec MCP).

```json
{
  "mcpServers": {
    "mi_servidor": {
      "type": "http",
      "url": "http://localhost:9000/mcp"
    }
  }
}
```

## Habilitacion de servidores

Los servidores en `.mcp.json` **no se activan automaticamente**. Claude Code
los registra pero requiere habilitacion explicita:

- Al iniciar Claude Code deberia preguntar si habilitar servidores nuevos.
- Desde terminal interactiva: `/mcp` para ver y habilitar servidores.
- Via CLI: `claude mcp add --transport sse nombre http://localhost:PORT/sse`

La habilitacion se persiste en `~/.claude.json` bajo la ruta del proyecto
en el campo `enabledMcpjsonServers`. Si un servidor no aparece como tool,
verificar este campo:

```json
{
  "projects": {
    "/ruta/del/proyecto": {
      "enabledMcpjsonServers": ["dbQuery", "bancos_mcp"]
    }
  }
}
```

## Pasos de instalacion — dbQuery (stdio)

1. Ejecutar `examples/install-db-query.ps1` desde la raiz del proyecto.
2. Ejecutar `node .mcp/db-query/tests/smoke.mjs`.
3. Reiniciar la sesion de Claude Code despues de guardar `.mcp.json`.
4. Confirmar que aparece `mcp__dbQuery__db_query`.

Mantener `env` vacio; las credenciales pertenecen a `.local-secrets/db.json`.

## Pasos de instalacion — servidor SSE (ej. Bancos.Mcp)

1. Levantar el servidor: `pwsh .mcp/bancos-mcp.ps1`
2. Verificar SSE: `curl -sN --max-time 3 http://localhost:8000/mcp/sse`
   Debe responder `event: endpoint` con URL de mensajes.
3. Agregar entrada en `.mcp.json` con `"type": "sse"`.
4. Reiniciar Claude Code **con el servidor ya corriendo**.
5. Habilitar con `/mcp` si no se activo automaticamente.
6. Confirmar que aparecen los tools como `mcp__bancos_mcp__process_import_file`.

## Troubleshooting

| Sintoma | Causa | Solucion |
|---|---|---|
| Tools no aparecen en deferred tools | Servidor no habilitado | Verificar `enabledMcpjsonServers` en `~/.claude.json` |
| Tools no aparecen tras habilitar | Servidor no estaba arriba al iniciar | Reiniciar Claude Code con servidor corriendo |
| `mcpServers` en settings.json ignorado | Archivo incorrecto | Mover a `.mcp.json` en raiz del proyecto |
| SSE no conecta | URL incorrecta | Verificar con `curl -sN` que devuelve `event: endpoint` |
| Error de conexion HTTPS | Servidor local sin TLS | Usar `http://` — localhost no requiere HTTPS |
