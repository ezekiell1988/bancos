# MCP para ChatGPT — Checklist y Errores Comunes

## Checklist de Validación

### Protocolo
- [ ] `initialize` devuelve `Mcp-Session-Id` en el response header
- [ ] `notifications/initialized` retorna HTTP 202 sin body JSON
- [ ] `MCP-Protocol-Version` header se valida en requests post-initialize (→ 400 si inválido)
- [ ] Header `Origin` se valida contra whitelist (→ 403 si no está en lista)
- [ ] `DELETE /mcp` limpia la sesión del IMemoryCache (→ 200 OK)
- [ ] `GET /mcp` retorna 200 OK (health check)

### Autenticación
- [ ] `Authorization: Bearer <api-key>` se valida en todas las requests
- [ ] Sin auth → HTTP 401
- [ ] API key almacenada en `appsettings.json` via `IOptions<McpOptions>` + secret en GitHub/Azure
- [ ] Variable de entorno agregada al workflow: `Mcp__ApiKey=${{ secrets.MCP_API_KEY }}`

### Tools
- [ ] Todas las tools tienen `outputSchema` declarado en `GetDefinitions()`
- [ ] Respuestas de tools incluyen `structuredContent` además de `content[0].text`
- [ ] `structuredContent` cumple el `outputSchema` declarado
- [ ] `content[0].text` siempre presente como fallback
- [ ] Descriptions de tools son específicas y mencionan cuándo usarlas

### Deploy
- [ ] HTTPS obligatorio — URL configurada en ChatGPT es `https://`
- [ ] `dotnet build` sin warnings ni errores
- [ ] Variable `Mcp__ApiKey` agregada al step `--set-env-vars` en `azure-deploy.yml`

---

## Comandos de Prueba con curl

### Health check
```bash
curl -s https://voicebot.clickeat.online/mcp
# → "MCP ready"
```

### Sin auth → 401
```bash
curl -sk https://voicebot.clickeat.online/mcp -X POST \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-06-18"}}'
# → HTTP 401
```

### Initialize con auth → Mcp-Session-Id en headers
```bash
curl -isk https://voicebot.clickeat.online/mcp -X POST \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer tu-api-key" \
  -d '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-06-18","clientInfo":{"name":"test","version":"1.0"}}}'
# → 200 OK
# → Header: Mcp-Session-Id: <guid>
# → Body: {"jsonrpc":"2.0","id":1,"result":{"protocolVersion":"2025-06-18",...}}
```

### tools/list con session
```bash
curl -sk https://voicebot.clickeat.online/mcp -X POST \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer tu-api-key" \
  -H "Mcp-Session-Id: <guid-del-initialize>" \
  -H "MCP-Protocol-Version: 2025-06-18" \
  -d '{"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}'
```

### DELETE session
```bash
curl -sk https://voicebot.clickeat.online/mcp -X DELETE \
  -H "Authorization: Bearer tu-api-key" \
  -H "Mcp-Session-Id: <guid>"
# → 200 OK
```

### Versión no soportada → 400
```bash
curl -sk https://voicebot.clickeat.online/mcp -X POST \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer tu-api-key" \
  -H "Mcp-Session-Id: <guid>" \
  -H "MCP-Protocol-Version: 2020-01-01" \
  -d '{"jsonrpc":"2.0","id":3,"method":"tools/list","params":{}}'
# → HTTP 400
```

---

## Errores Comunes

| Síntoma | Causa | Solución |
|---------|-------|----------|
| ChatGPT dice "No se pudo conectar" | URL incorrecta o sin HTTPS | Verificar URL con curl |
| HTTP 401 en ChatGPT | API key no configurada en la UI de ChatGPT | Settings → Complementos → editar API key |
| `tools/list` vacío | `GetDefinitions()` retorna lista vacía | Verificar DI registration en ServiceExtensions |
| `structuredContent` ignorado | `outputSchema` no declarado en tool definition | Agregar `OutputSchema` al `McpToolDefinition` |
| Tool no se activa | Description poco descriptiva | Mejorar la description para el LLM |
| Session expirada | TTL de 30min cumplido | Llamar `start_session` / `initialize` de nuevo |
| HTTP 403 en ChatGPT | Origin de ChatGPT no en whitelist | Agregar `https://chatgpt.com` a `AllowedOrigins` |
| HTTP 400 en tools/list | `MCP-Protocol-Version` inválido | Verificar versiones soportadas en el servidor |
