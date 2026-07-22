# MCP — Diseño de Tools

## 1. Estructura de una Tool

Una tool tiene tres responsabilidades:
1. **Definición** (`GetDefinition`): qué hace, qué parámetros acepta → para `tools/list`
2. **Validación de acceso** (`IsPrivate`): control público/privado
3. **Ejecución** (`ExecuteAsync`): lógica de negocio → para `tools/call`

### Interfaz C#

```csharp
// Tools/IMcpTool.cs
public interface IMcpTool
{
    bool IsPrivate { get; }
    McpTool GetDefinition();
    Task<string> ExecuteAsync(JsonElement arguments, string? userEmail, CancellationToken ct);
}
```

---

## 2. JSON Schema del `inputSchema`

```json
{
  "type": "object",
  "properties": {
    "param_string": {
      "type": "string",
      "description": "Descripción detallada para que el LLM sepa qué valor pasar"
    },
    "param_int": {
      "type": "integer",
      "description": "...",
      "minimum": 1,
      "maximum": 100,
      "default": 10
    },
    "param_enum": {
      "type": "string",
      "enum": ["opcion_a", "opcion_b", "opcion_c"],
      "description": "..."
    },
    "param_opcional": {
      "type": "string",
      "description": "..."
    }
  },
  "required": ["param_string"]
}
```

**Reglas para la `description` de cada parámetro:**
- Ser específico: qué formato espera, ejemplos concretos
- El LLM infiere el valor a partir de la conversación usando esta descripción
- No usar genéricos como "el parámetro de entrada"

---

## 3. Resultado de una Tool

El resultado **siempre** tiene este formato:
```json
{
  "content": [
    {
      "type": "text",
      "text": "<cualquier string, JSON, tabla, etc.>"
    }
  ]
}
```

**Tips para el texto del resultado:**
- **Menos tokens = mejor**: el resultado se inyecta al contexto del LLM
- Evitar JSON verboso con muchas llaves y comillas innecesarias
- Para tablas de datos, usar formato delimitado por `|` o CSV
- Para objetos únicos, usar `clave: valor` en líneas separadas

---

## 4. Control de Acceso (Público / Privado)

```
PÚBLICA  → Visible y ejecutable sin autenticación
PRIVADA  → Solo si el usuario tiene email autenticado verificable
```

El servidor decide la visibilidad en `tools/list` y rechaza en `tools/call` si no hay acceso.

---

## 5. Optimización de Tokens en Respuestas

Los resultados de las tools se inyectan al contexto del LLM. Menos tokens = mejor rendimiento y menor costo.

**Formato TOON (Token-Oriented Object Notation)** — Para tablas de datos:
```
Arrays de objetos homogéneos:

JSON (verboso):
[{"id":1,"nombre":"Ana","email":"ana@x.com"},{"id":2,"nombre":"Luis","email":"luis@x.com"}]

TOON (compacto, ~40% menos tokens):
[2]{id,nombre,email}:
  1,Ana,ana@x.com
  2,Luis,luis@x.com
```

**Reglas de optimización:**
- Para un solo objeto → `clave: valor` en líneas separadas
- Para listas de objetos → encabezado con columnas + filas CSV
- Para texto narrativo → plain text, sin envolver en JSON
- Evitar anidar objetos cuando se puede aplanar
