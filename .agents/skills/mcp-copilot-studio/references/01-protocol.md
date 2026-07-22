# MCP — Protocolo y Flujo de Comunicación

## 1. Fundamentos del Protocolo

### ¿Qué es MCP?
MCP (Model Context Protocol) es un protocolo abierto que permite a los LLMs descubrir y ejecutar "herramientas" (tools) expuestas por un servidor externo. Copilot Studio actúa como **cliente MCP** llamando a tu servidor.

### Transport: Streamable HTTP (único compatible con Copilot Studio)
Copilot Studio **solo soporta** el transporte **Streamable HTTP** (HTTP sincrón estándar).
- ❌ No usar SSE (Server-Sent Events) permanente
- ❌ No usar WebSockets
- ✅ Un único endpoint HTTP que acepta `POST` y `GET`

### Protocolo base: JSON-RPC 2.0
Toda comunicación usa JSON-RPC 2.0:

```json
// Solicitud cliente → servidor
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "tools/call",
  "params": { "name": "mi_tool", "arguments": { "param1": "valor" } }
}

// Respuesta servidor → cliente
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": { ... }
}

// Respuesta de error servidor → cliente
{
  "jsonrpc": "2.0",
  "id": 1,
  "error": { "code": -32601, "message": "Method not found" }
}
```

### Versiones de Protocolo Compatibles con Copilot Studio
```
MCP_PROTOCOL_VERSION = "2025-06-18"
COMPATIBLE_MCP_VERSIONS = ["2024-11-05", "2025-06-18"]
```
Siempre responder con la **misma versión que el cliente envía** si es compatible; si no, usar `"2025-06-18"`.

---

## 2. Flujo Completo de Comunicación

Copilot Studio sigue esta secuencia al conectarse:

```
1. POST /  → method: "initialize"                  ← Handshake del protocolo
2. POST /  → method: "notifications/initialized"   ← ACK del cliente (responder 202)
3. POST /  → method: "tools/list"                  ← Descubrir herramientas disponibles
4. POST /  → method: "tools/call"                  ← Ejecutar una herramienta
5. GET  /                                           ← Health check (responder 200 OK)
```

### 2.1 Método `initialize`

**Request del cliente:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "initialize",
  "params": {
    "protocolVersion": "2025-06-18",
    "clientInfo": { "name": "copilot-studio", "version": "1.0.0" },
    "capabilities": {}
  }
}
```

**Respuesta del servidor (OBLIGATORIA):**
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
      "name": "mi-servidor-mcp",
      "version": "1.0.0"
    }
  }
}
```
- `capabilities.tools.listChanged: false` → las herramientas son estáticas (no cambian en tiempo real)
- `capabilities.tools.listChanged: true` → el servidor puede notificar cambios (para uso avanzado)

### 2.2 Método `notifications/initialized`

El cliente avisa que recibió el initialize. El servidor **debe responder `HTTP 202 Accepted`** con cuerpo vacío. No retornar JSON aquí.

```
HTTP/1.1 202 Accepted
```

### 2.3 Método `tools/list`

**Request:**
```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "method": "tools/list",
  "params": {}
}
```

**Respuesta:**
```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "result": {
    "tools": [
      {
        "name": "buscar_cliente",
        "title": "Buscar Cliente",
        "description": "Busca un cliente en el CRM por nombre o email. Retorna datos de contacto y cuenta.",
        "inputSchema": {
          "type": "object",
          "properties": {
            "query": {
              "type": "string",
              "description": "Nombre, email o teléfono del cliente a buscar"
            },
            "limite": {
              "type": "integer",
              "description": "Máximo de resultados a retornar (default: 5)",
              "default": 5,
              "minimum": 1,
              "maximum": 20
            }
          },
          "required": ["query"]
        }
      }
    ]
  }
}
```

**Campos críticos de cada tool:**
| Campo | Requerido | Descripción |
|-------|-----------|-------------|
| `name` | ✅ | Identificador único, snake_case, sin espacios |
| `title` | ✅ | Nombre legible para el usuario |
| `description` | ✅ | **MUY IMPORTANTE**: el LLM decide cuándo usar la tool basándose en esto |
| `inputSchema` | ✅ | JSON Schema del objeto de entrada |

### 2.4 Método `tools/call`

**Request:**
```json
{
  "jsonrpc": "2.0",
  "id": 3,
  "method": "tools/call",
  "params": {
    "name": "buscar_cliente",
    "arguments": {
      "query": "Juan Pérez",
      "limite": 3
    }
  }
}
```

**Respuesta exitosa:**
```json
{
  "jsonrpc": "2.0",
  "id": 3,
  "result": {
    "content": [
      {
        "type": "text",
        "text": "Juan Pérez | juan@email.com | +506 8888-0000\nJuan Pérez Mora | juan.mora@email.com | +506 7777-0000"
      }
    ]
  }
}
```

**CRÍTICO**: El resultado siempre debe ir dentro de `content[0].text` como string. El campo `type` siempre es `"text"`.

### 2.5 Códigos de Error JSON-RPC 2.0

| Código | Nombre | Usar cuando |
|--------|--------|-------------|
| `-32700` | Parse error | JSON malformado |
| `-32600` | Invalid Request | No cumple JSON-RPC 2.0 |
| `-32601` | Method not found | Método desconocido |
| `-32602` | Invalid params | Tool desconocida o argumentos inválidos |
| `-32603` | Internal error | Error inesperado en la tool |

> Siempre retornar `HTTP 200 OK` incluso para errores JSON-RPC (el error va en el body, no en el status HTTP). La única excepción es `202 Accepted` para notificaciones.
