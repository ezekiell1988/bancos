# ChatGPT / OpenAI — Requisitos Específicos

## 1. Autenticación

ChatGPT soporta dos métodos de autenticación para MCP servers:

### 1.1 API Key (recomendado para servidores propios)

- El usuario configura una API key en la UI de ChatGPT al agregar el MCP server
- ChatGPT envía la key en el header `Authorization: Bearer <api-key>`
- El servidor valida contra una key almacenada en configuración

```csharp
// Validación simple de API key
var apiKey = httpReq.Headers.Authorization.ToString().Replace("Bearer ", "");
if (apiKey != options.Value.ApiKey)
    return Results.Unauthorized();
```

**Configuración en ChatGPT:**
1. Settings → Complementos → + Agregar
2. URL: `https://tu-servidor.com/mcp`
3. Autorización: API Key / Bearer Token
4. Ingresar la key

### 1.2 OAuth 2.0

- Para escenarios multi-tenant o con identidad de usuario
- ChatGPT actúa como OAuth client, obtiene token del authorization server
- El servidor valida el JWT Bearer token
- Más complejo pero permite identificar al usuario

### 1.3 Sin autenticación (solo desarrollo)

- ChatGPT permite conectar sin auth en modo desarrollo
- **Nunca** dejar sin auth en producción — cualquiera podría invocar las tools

---

## 2. Diferencias con Copilot Studio

| Aspecto | ChatGPT | Copilot Studio |
|---------|---------|----------------|
| Transport | Streamable HTTP (POST /mcp) | Streamable HTTP (POST /) |
| Batch malformado | No — requests bien formadas | Sí — envía `[{"jsonrpc":"2.0"}]` |
| Wildcard route | No necesario — ruta fija `/mcp` | Sí — requiere `/{**slug}` |
| `routePrefix` | No aplica | Obligatorio vacío en Azure Functions |
| Headers identidad | `Authorization: Bearer` | `x-ms-client-object-id` (sin OAuth) |
| `output_schema` | Sí — usado para validación y citaciones | No lo usa |
| `structuredContent` | Sí — para UI rica y citaciones | No lo usa |
| `Mcp-Session-Id` | Sí — spec 2025-06-18 | Puede ignorarlo |
| `MCP-Protocol-Version` | Sí — spec 2025-06-18 | Puede ignorarlo |
| `title` en tools | Opcional | Recomendado |
| DELETE session | Sí | No lo envía |

---

## 3. `outputSchema` en Tool Definitions

ChatGPT usa `outputSchema` para:
- Validar que la respuesta de la tool cumple el schema
- Generar citaciones automáticas
- Renderizar datos en UI rica (tablas, cards)

```json
{
  "name": "get_menu",
  "description": "Obtiene el menú de un restaurante",
  "inputSchema": { ... },
  "outputSchema": {
    "type": "object",
    "properties": {
      "restaurantName": { "type": "string" },
      "products": {
        "type": "array",
        "items": {
          "type": "object",
          "properties": {
            "id": { "type": "integer" },
            "name": { "type": "string" },
            "price": { "type": "number" },
            "category": { "type": "string" }
          }
        }
      }
    }
  }
}
```

**Reglas:**
- El `outputSchema` describe lo que va en `structuredContent`, no en `content[0].text`
- Usar tipos JSON Schema estándar: `string`, `integer`, `number`, `boolean`, `array`, `object`
- No usar `$ref` ni schemas externos — todo inline
- Mantener schemas simples y planos cuando sea posible

---

## 4. `structuredContent` en Respuestas

Además de `content[0].text` (obligatorio como fallback), incluir `structuredContent`:

```json
{
  "jsonrpc": "2.0",
  "id": 3,
  "result": {
    "content": [
      {
        "type": "text",
        "text": "Restaurante: Pizza Hut Escazú\n1. Pepperoni Personal ₡4500\n2. Hawaiana Mediana ₡7200"
      }
    ],
    "structuredContent": {
      "restaurantName": "Pizza Hut Escazú",
      "products": [
        { "id": 101, "name": "Pepperoni Personal", "price": 4500, "category": "Pizzas" },
        { "id": 102, "name": "Hawaiana Mediana", "price": 7200, "category": "Pizzas" }
      ]
    }
  }
}
```

**Reglas:**
- `structuredContent` DEBE cumplir el `outputSchema` declarado en la tool definition
- `content[0].text` sigue siendo obligatorio — es el fallback para clientes que no soportan `structuredContent`
- El `text` puede ser una versión legible del mismo dato o el JSON serializado
- Si no se declara `outputSchema`, `structuredContent` se ignora

---

## 5. Configuración en ChatGPT

### Agregar MCP Server

1. ChatGPT → Settings → Complementos → `+`
2. Nombre: descriptivo (ej: "Cleo - ClickEat")
3. URL: `https://tu-servidor.com/mcp`
4. Autorización: Bearer Token / API Key
5. Guardar → Estado cambia a "Conectado"

### Visibilidad

- `public` — cualquier usuario de ChatGPT puede usar el complemento
- `private` — solo el creador
- `team` — miembros del workspace

### Estado de revisión

- `development` — en desarrollo, sin revisión
- `approved` — revisado y aprobado por OpenAI

---

## 6. Latencia y Rendimiento

- ChatGPT tiene un timeout de ~60 segundos para tools/call
- Entre cada tool call, ChatGPT "piensa" 5-50 segundos (no controlable por el servidor)
- Optimizar respuestas para minimizar tokens (el resultado se inyecta al contexto del LLM)
- Para operaciones largas, considerar responder rápido con estado y polling con otra tool

---

## 7. Errores Comunes con ChatGPT

| Síntoma | Causa | Solución |
|---------|-------|----------|
| "No se pudo conectar" | URL incorrecta o servidor caído | Verificar URL con `curl -X POST` |
| "Error de autenticación" | API key incorrecta o expirada | Verificar key en Settings |
| "Tool no encontrada" | `tools/list` no retorna la tool | Verificar nombre exacto |
| "Error interno" | Excepción en el servidor | Revisar logs del servidor |
| Respuesta vacía | `content[0].text` vacío | Siempre retornar texto útil |
| Tool no se activa | `description` poco descriptiva | Mejorar la descripción para el LLM |
| Loop infinito | Tool retorna datos ambiguos | Hacer respuestas claras y definitivas |
