# MCP para ChatGPT — Diseño de Tools

## 1. Estructura de una Tool

Cada tool tiene:
1. **Definition** → `tools/list`: nombre, descripción, inputSchema, outputSchema
2. **Execution** → `tools/call`: lógica de negocio, retorna content + structuredContent

### Interfaz C# (patrón IMcpToolProvider)

```csharp
public sealed record McpToolDefinition(
    string Name,
    string Description,
    object InputSchema,
    object? OutputSchema = null);

public interface IMcpToolProvider
{
    IReadOnlyList<McpToolDefinition> GetDefinitions();
    Task<string> ExecuteAsync(string toolName, JsonElement arguments, CancellationToken ct);
}
```

Para soportar `structuredContent`, extender el retorno:

```csharp
public sealed record McpToolResult(string Text, object? StructuredContent = null);

public interface IMcpToolProviderV2
{
    IReadOnlyList<McpToolDefinition> GetDefinitions();
    Task<McpToolResult> ExecuteAsync(string toolName, JsonElement arguments, CancellationToken ct);
}
```

---

## 2. inputSchema — JSON Schema de Entrada

```json
{
  "type": "object",
  "properties": {
    "phone": {
      "type": "string",
      "description": "Teléfono del cliente, 8 dígitos, sin código de país. Ejemplo: 88001234"
    },
    "deliveryType": {
      "type": "string",
      "enum": ["pickup", "delivery"],
      "description": "Tipo de entrega: pickup (recoger en restaurante) o delivery (envío a domicilio)"
    },
    "limit": {
      "type": "integer",
      "description": "Máximo de resultados",
      "default": 10,
      "minimum": 1,
      "maximum": 50
    }
  },
  "required": ["phone"]
}
```

**Reglas para descriptions:**
- Ser específico con formato esperado y ejemplos
- El LLM infiere el valor de la conversación usando la description
- Incluir constraints (largo, formato, valores válidos)

---

## 3. outputSchema — JSON Schema de Salida

Define la estructura de `structuredContent`. ChatGPT lo usa para:
- Validar respuestas
- Generar citaciones
- Renderizar UI rica

```json
{
  "type": "object",
  "properties": {
    "sessionId": {
      "type": "string",
      "description": "ID de sesión para usar en calls posteriores"
    },
    "message": {
      "type": "string",
      "description": "Mensaje de confirmación"
    }
  },
  "required": ["sessionId", "message"]
}
```

**Reglas:**
- Todo inline, sin `$ref`
- Tipos simples: `string`, `integer`, `number`, `boolean`, `array`, `object`
- Para arrays, definir `items` con el schema de cada elemento
- Mantener plano cuando sea posible — evitar anidamiento profundo

---

## 4. Respuesta de Tool — content + structuredContent

```csharp
// En el dispatch, construir la respuesta con ambos formatos
var toolResult = await provider.ExecuteAsync(toolName, argsEl, ct);

return JsonRpc(id, new
{
    content = new[] { new { type = "text", text = toolResult.Text } },
    structuredContent = toolResult.StructuredContent
});
```

**content[0].text** — Formato legible o JSON serializado. Siempre presente.
**structuredContent** — Objeto tipado que cumple `outputSchema`. Puede ser null si la tool no declara outputSchema.

---

## 5. Optimización de Tokens (TOON)

Los resultados se inyectan al contexto del LLM. Menos tokens = mejor rendimiento y menor costo.

**Formato TOON para content[0].text:**
```
JSON verboso:
[{"id":1,"nombre":"Ana","email":"ana@x.com"},{"id":2,"nombre":"Luis","email":"luis@x.com"}]

TOON compacto (~40% menos tokens):
[2]{id,nombre,email}:
  1,Ana,ana@x.com
  2,Luis,luis@x.com
```

**Reglas:**
- Para un solo objeto → `clave: valor` en líneas separadas
- Para listas → encabezado con columnas + filas CSV
- Para texto narrativo → plain text
- `structuredContent` no necesita optimización — es para la máquina, no para el contexto LLM

---

## 6. Descripciones Efectivas para ChatGPT

La `description` de cada tool es **crítica** — ChatGPT decide cuándo invocar la tool basándose en ella.

**Buena:**
```
"Busca un cliente por número de teléfono. Retorna nombre, dirección y pedidos recientes. Llamar antes de crear un pedido para verificar si el cliente existe."
```

**Mala:**
```
"Busca cliente"
```

**Tips:**
- Describir qué hace Y cuándo usarla
- Mencionar qué retorna
- Indicar dependencias (ej: "llamar después de start_session")
- Incluir el flujo esperado si hay orden entre tools
