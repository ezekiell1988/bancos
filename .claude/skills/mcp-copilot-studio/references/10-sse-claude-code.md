# MCP SSE Transport — Integración con Claude Code

## Contexto

Copilot Studio usa Streamable HTTP (POST/GET a un solo endpoint `/mcp`).
Claude Code usa **SSE (Server-Sent Events)** o **Streamable HTTP** como transporte.
Ambos pueden coexistir en el mismo servidor ASP.NET con rutas separadas.

## Arquitectura dual en Bancos.Mcp

```
Copilot Studio  →  POST /mcp          →  McpHandler.cs     (Streamable HTTP)
Claude Code     →  GET  /mcp/sse      →  McpSseHandler.cs  (SSE transport)
                   POST /mcp/sse/message?sessionId=X
```

Las rutas SSE deben registrarse **antes** de los catch-all de Copilot Studio:

```csharp
endpoints.MapGet("/mcp/sse", McpSseHandler.HandleSseAsync);
endpoints.MapPost("/mcp/sse/message", McpSseHandler.HandleMessageAsync);
endpoints.MapPost("/{**path}", McpHandler.HandleAsync);  // catch-all después
```

## Protocolo SSE — Flujo completo

1. **Cliente abre GET `/mcp/sse`** — servidor responde con `Content-Type: text/event-stream`
2. **Servidor envía evento `endpoint`** con la URL para POST de mensajes:
   ```
   event: endpoint
   data: http://localhost:8000/mcp/sse/message?sessionId=abc123
   ```
3. **Cliente envía JSON-RPC vía POST** al endpoint recibido
4. **Servidor escribe respuesta como evento `message`** en el stream SSE:
   ```
   event: message
   data: {"jsonrpc":"2.0","id":1,"result":{...}}
   ```
5. La conexión SSE se mantiene abierta para recibir múltiples respuestas

## Implementación ASP.NET (.NET 10)

Componentes clave del `McpSseHandler.cs`:

- **`ConcurrentDictionary<string, SseSession>`** — mapa de sesiones activas por sessionId
- **`SseSession`** — usa `System.Threading.Channels.Channel<string>` para pasar mensajes del POST al stream SSE
- **GET handler** — genera sessionId, envía evento `endpoint`, luego lee del Channel indefinidamente
- **POST handler** — busca la sesión, procesa el JSON-RPC, escribe respuesta al Channel

```csharp
// Escritura de evento SSE
await response.WriteAsync($"event: {eventType}\ndata: {data}\n\n", ct);
await response.Body.FlushAsync(ct);
```

## Métodos JSON-RPC soportados

| Método | Descripción |
|---|---|
| `initialize` | Negocia versión de protocolo (`2024-11-05` o `2025-06-18`) |
| `tools/list` | Devuelve lista de tools registrados |
| `tools/call` | Ejecuta un tool por nombre con argumentos |
| `notifications/*` | Se acepta y descarta (no requiere respuesta) |

## Problema crítico: serialización camelCase

La spec MCP requiere que `tools/list` devuelva campos en **camelCase**:

```json
{"name": "my_tool", "description": "...", "inputSchema": {...}}
```

El serializer de .NET por defecto usa **PascalCase** para records:

```json
{"Name": "my_tool", "Description": "...", "InputSchema": {...}}
```

Claude Code **no reconoce tools con PascalCase** — aparecen en `tools/list` pero
no se registran como deferred tools. La solución:

```csharp
private static readonly JsonSerializerOptions CamelCase = new()
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
};

private static string JsonRpcResponse(JsonElement id, object result) =>
    JsonSerializer.Serialize(new { jsonrpc = "2.0", id, result }, CamelCase);
```

**Aplicar en ambos handlers** (McpHandler para Copilot Studio, McpSseHandler para Claude Code).

## Configuración en Claude Code

### 1. `.mcp.json` (raíz del proyecto)

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

- `type: "sse"` es obligatorio (sin él, Claude asume stdio)
- `http://` funciona en localhost — no requiere HTTPS
- La URL debe apuntar al endpoint SSE, no al endpoint Streamable HTTP

### 2. Habilitación en `~/.claude.json`

Los servidores en `.mcp.json` no se activan automáticamente. Se requiere habilitación:

```json
{
  "projects": {
    "/ruta/del/proyecto": {
      "enabledMcpjsonServers": ["iaWorkflow", "dbQuery", "bancos_mcp"]
    }
  }
}
```

Formas de habilitar:
- Claude Code pregunta al detectar servidores nuevos al iniciar
- Comando `/mcp` en terminal interactiva
- CLI: `claude mcp add --transport sse bancos_mcp http://localhost:8000/mcp/sse`
- Editar `~/.claude.json` manualmente

### 3. Archivos que NO funcionan para mcpServers

- `.claude/settings.json` — ignora `mcpServers` silenciosamente
- `.claude/settings.local.json` — mismo comportamiento

## Orden de inicio

1. Levantar el servidor MCP primero (`pwsh .mcp/bancos-mcp.ps1`)
2. Verificar SSE: `curl -sN --max-time 3 http://localhost:8000/mcp/sse` → debe devolver `event: endpoint`
3. Iniciar Claude Code — conecta al SSE al arrancar
4. Tools aparecen como `mcp__bancos_mcp__<tool_name>` en deferred tools

## Verificación

```bash
# 1. Verificar que SSE responde
curl -sN --max-time 3 http://localhost:8000/mcp/sse

# 2. Test completo con initialize + tools/list
(curl -sN http://localhost:8000/mcp/sse > /tmp/sse.txt &)
sleep 1
EP=$(grep "^data:" /tmp/sse.txt | head -1 | sed 's/^data: //')
curl -s -X POST "$EP" -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"test","version":"1"}}}'
sleep 0.5
curl -s -X POST "$EP" -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}'
sleep 1
cat /tmp/sse.txt
# Verificar que los campos sean camelCase: name, description, inputSchema

# 3. Verificar habilitación
python3 -c "
import json
with open('$HOME/.claude.json') as f:
    d = json.load(f)
print(d.get('projects',{}).get('$(pwd)',{}).get('enabledMcpjsonServers'))
"
```

## Troubleshooting

| Síntoma | Causa | Solución |
|---|---|---|
| Tools no aparecen en deferred tools | `enabledMcpjsonServers` vacío o sin el nombre | Agregar nombre al array en `~/.claude.json` |
| Tools en deferred pero no se cargan | Campos PascalCase en `tools/list` | Usar `JsonNamingPolicy.CamelCase` en serialización |
| `Session not found` en POST | Sesión SSE ya cerró | Mantener conexión SSE abierta antes de enviar POST |
| Servidor no detectado al iniciar Claude | Servidor no estaba corriendo | Iniciar servidor antes de Claude Code |
| `enabledMcpjsonServers` se borra | Claude Code lo resetea en ciertos flujos | Verificar después de cada reinicio |
| Hot reload no aplica cambios al handler SSE | dotnet watch no recarga ciertos cambios | Reiniciar servidor manualmente |
