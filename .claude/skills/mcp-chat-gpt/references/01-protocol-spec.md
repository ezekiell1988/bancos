# MCP Spec 2025-06-18 — Protocolo y Headers

## 1. Fundamentos del Protocolo

### Transport: Streamable HTTP

ChatGPT usa **Streamable HTTP** — un único endpoint HTTP que acepta:
- `POST` → JSON-RPC 2.0 requests (initialize, tools/list, tools/call)
- `GET` → Abre conexión SSE para server-initiated messages (opcional, spec compliance)
- `DELETE` → Termina una sesión explícitamente

### Protocolo base: JSON-RPC 2.0

```json
// Request cliente → servidor
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "tools/call",
  "params": { "name": "mi_tool", "arguments": { "param1": "valor" } }
}

// Response servidor → cliente
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": { ... }
}
```

### Versiones de Protocolo

```
COMPATIBLE_VERSIONS = ["2024-11-05", "2025-06-18"]
DEFAULT_VERSION = "2025-06-18"
```

---

## 2. Headers Obligatorios (Spec 2025-06-18)

### 2.1 `Mcp-Session-Id` (servidor → cliente → servidor)

- El servidor DEBE generar un `Mcp-Session-Id` único en la respuesta de `initialize`
- El cliente DEBE enviarlo en todas las requests posteriores como header
- El servidor DEBE validar que el sessionId existe; si no → HTTP 404
- Implementación: UUID o GUID, asociado al estado en IMemoryCache

```
// Response de initialize
HTTP/1.1 200 OK
Mcp-Session-Id: a1b2c3d4e5f6
Content-Type: application/json

{"jsonrpc":"2.0","id":1,"result":{...}}
```

```
// Request posterior del cliente
POST /mcp HTTP/1.1
Mcp-Session-Id: a1b2c3d4e5f6
MCP-Protocol-Version: 2025-06-18
Content-Type: application/json
```

### 2.2 `MCP-Protocol-Version` (cliente → servidor)

- El cliente DEBE enviar este header en todas las requests **después** de initialize
- El servidor DEBE validarlo:
  - Si está presente y no es soportado → HTTP 400
  - Si no está presente en request post-initialize → HTTP 400 (estricto) o tolerarlo (pragmático)
- En `initialize` este header NO se envía (la versión va en `params.protocolVersion`)

### 2.3 `Origin` (cliente → servidor)

- El servidor DEBE validar el header `Origin` para prevenir DNS rebinding
- Configurar whitelist de orígenes permitidos
- Si `Origin` no está en la whitelist → HTTP 403

```csharp
// Ejemplo whitelist
var allowedOrigins = new[] {
    "https://chatgpt.com",
    "https://copilot.microsoft.com",
    "https://voicebot.clickeat.online"
};
```

### 2.4 `Accept` (cliente → servidor)

- El cliente envía `Accept: application/json, text/event-stream`
- El servidor puede responder con:
  - `application/json` → respuesta JSON-RPC directa (lo más común con ChatGPT)
  - `text/event-stream` → respuesta como stream SSE (para tools de larga duración)
- Para la mayoría de tools, responder con `application/json` es suficiente

---

## 3. Flujo Completo

```
1. POST /mcp  → method: "initialize"                  ← Handshake
   ← Response con Mcp-Session-Id header

2. POST /mcp  → method: "notifications/initialized"   ← ACK (202, sin body)
   Header: Mcp-Session-Id + MCP-Protocol-Version

3. POST /mcp  → method: "tools/list"                  ← Descubrir tools
   Header: Mcp-Session-Id + MCP-Protocol-Version

4. POST /mcp  → method: "tools/call"                  ← Ejecutar tool
   Header: Mcp-Session-Id + MCP-Protocol-Version

5. DELETE /mcp                                         ← Terminar sesión
   Header: Mcp-Session-Id
```

### 3.1 `initialize`

**Request:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "initialize",
  "params": {
    "protocolVersion": "2025-06-18",
    "clientInfo": { "name": "chatgpt", "version": "1.0.0" },
    "capabilities": {}
  }
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "protocolVersion": "2025-06-18",
    "capabilities": {
      "tools": { "listChanged": false }
    },
    "serverInfo": {
      "name": "voicebot-purchase",
      "version": "1.0.0"
    }
  }
}
```

### 3.2 `notifications/initialized` → HTTP 202

```
HTTP/1.1 202 Accepted
```

### 3.3 `tools/list` → incluir `outputSchema`

```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "result": {
    "tools": [
      {
        "name": "get_customer",
        "description": "Busca un cliente por teléfono",
        "inputSchema": {
          "type": "object",
          "properties": {
            "phone": { "type": "string", "description": "Teléfono 8 dígitos" }
          },
          "required": ["phone"]
        },
        "outputSchema": {
          "type": "object",
          "properties": {
            "customerId": { "type": "integer" },
            "name": { "type": "string" },
            "phone": { "type": "string" }
          },
          "required": ["customerId", "name", "phone"]
        }
      }
    ]
  }
}
```

### 3.4 `tools/call` → incluir `structuredContent`

```json
{
  "jsonrpc": "2.0",
  "id": 3,
  "result": {
    "content": [
      { "type": "text", "text": "{\"customerId\":123,\"name\":\"Juan\",\"phone\":\"88880000\"}" }
    ],
    "structuredContent": {
      "customerId": 123,
      "name": "Juan",
      "phone": "88880000"
    }
  }
}
```

### 3.5 DELETE → Terminar sesión

```
DELETE /mcp HTTP/1.1
Mcp-Session-Id: a1b2c3d4e5f6

→ HTTP 200 OK (sesión eliminada)
→ HTTP 404 Not Found (sesión no existe)
```

---

## 4. Códigos de Error JSON-RPC 2.0

| Código | Nombre | Usar cuando |
|--------|--------|-------------|
| `-32700` | Parse error | JSON malformado |
| `-32600` | Invalid Request | No cumple JSON-RPC 2.0 |
| `-32601` | Method not found | Método desconocido |
| `-32602` | Invalid params | Tool desconocida o argumentos inválidos |
| `-32603` | Internal error | Error inesperado en la tool |

> HTTP 200 para errores JSON-RPC (el error va en el body). Excepciones: 202 para notificaciones, 400 para versión inválida, 403 para origin inválido, 404 para sesión no encontrada.
