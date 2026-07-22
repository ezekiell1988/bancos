# MCP — Quirks de Copilot Studio y Autenticación

## 1. Quirks Específicos de Copilot Studio 🔴

Estos problemas son **exclusivos de Copilot Studio** y no ocurren con otros clientes MCP:

### 1.1 Batch Request Malformado al Descubrir Herramientas

**Problema**: Copilot Studio a veces envía el body como un **array JSON** en lugar de un objeto:
```json
[{"jsonrpc": "2.0"}]
```

**Solución C#**: Detectar arrays y normalizarlos a una solicitud válida:

```csharp
// McpHandler.cs
private static JsonElement NormalizeBatch(JsonElement root)
{
    if (root.ValueKind != JsonValueKind.Array)
        return root;

    if (root.GetArrayLength() != 1)
        throw new InvalidOperationException("Batch multi-elemento no soportado");

    var first = root[0];
    var hasMethod  = first.TryGetProperty("method",  out _);
    var hasId      = first.TryGetProperty("id",      out var idProp);
    var isJsonRpc2 = first.TryGetProperty("jsonrpc", out var jrpc) && jrpc.GetString() == "2.0";

    if (!isJsonRpc2) throw new InvalidOperationException("jsonrpc != 2.0");

    // Solo {"jsonrpc":"2.0"} → convertir a initialize
    if (!hasMethod && !hasId)
        return JsonDocument.Parse("""
            {
              "jsonrpc":"2.0","id":1,"method":"initialize",
              "params":{"protocolVersion":"2025-06-18",
                        "clientInfo":{"name":"copilot-studio","version":"1.0.0"}}
            }
            """).RootElement;

    // Tiene id pero no method → convertir a tools/list
    if (!hasMethod)
        return JsonDocument.Parse($$"""
            {"jsonrpc":"2.0","id":{{idProp.GetRawText()}},"method":"tools/list","params":{}}
            """).RootElement;

    return first;
}
```

### 1.2 El Cliente Puede Enviar Respuestas al Servidor

Copilot Studio a veces envía un mensaje que tiene `id` pero **no tiene `method`**. Esto es una respuesta del cliente, no una solicitud. El servidor debe responder `202 Accepted` sin cuerpo.

```csharp
// Detectar respuesta del cliente (tiene id, no tiene method)
if (method is null && id.HasValue)
    return Results.StatusCode(202);
```

### 1.3 Route Prefix Vacío (Azure Functions)

En `host.json`, es **obligatorio** poner el `routePrefix` vacío para que Copilot Studio pueda llamar directamente a `/`:

```json
{
  "version": "2.0",
  "extensions": {
    "http": {
      "routePrefix": ""
    }
  }
}
```
Sin esto, Azure Functions agrega el prefijo `/api/` y las URLs no coinciden con lo configurado en Copilot Studio.

> **Nota**: Con ASP.NET Core Minimal APIs este problema no existe porque no hay prefijo automático.

### 1.4 Wildcard Route

El endpoint debe aceptar **cualquier ruta** porque Copilot Studio puede llamar con rutas inesperadas:

```csharp
// ASP.NET Core Minimal APIs
app.MapGet( "/{**slug}", McpHandler.HealthCheck);
app.MapPost("/{**slug}", McpHandler.HandleAsync);
```

```csharp
// Azure Functions .NET 10 Isolated
[Function("McpEndpoint")]
public async Task<HttpResponseData> Run(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "{*route}")] HttpRequestData req)
{ ... }
```

---

## 2. Headers de Autenticación de Copilot Studio

Copilot Studio inyecta automáticamente headers de identidad del usuario. Su disponibilidad depende del tipo de conector:

| Header | Descripción | Sin Security | Con OAuth |
|--------|-------------|:---:|:---:|
| `x-ms-client-object-id` | AAD Object ID del usuario (GUID) | ✅ | ✅ |
| `x-ms-client-principal-name` | UPN del usuario (`user@tenant.com`) | ✅ | ✅ |
| `x-ms-client-tenant-id` | Tenant ID de Azure AD del usuario | ✅ | ✅ |
| `Authorization: Bearer <jwt>` | Token OAuth del usuario | ❌ | ✅ |
| `X-MS-CLIENT-PRINCIPAL` | Claims completos en base64 (EasyAuth) | ❌ | Depende |

> **Clave**: El conector **"Sin Security"** SÍ envía los tres primeros headers. Son del usuario autenticado en Copilot Studio, no del bot. El `Authorization` header solo existe cuando el conector tiene OAuth configurado.

> Para el patrón completo de extracción de identidad, enriquecimiento con Graph y validación de permisos, ver **[07-identity-graph.md](07-identity-graph.md)**.

### Extracción del email del JWT (sin verificar firma, solo para logging/personalización)

```csharp
// McpHandler.cs
private static string? ExtractUserEmail(HttpRequest req)
{
    // Intentar EasyAuth primero
    if (req.Headers.TryGetValue("X-MS-CLIENT-PRINCIPAL-NAME", out var name))
        return name.ToString();

    // Fallback: decodificar JWT sin verificar firma
    if (!req.Headers.TryGetValue("Authorization", out var auth))
        return null;

    var token = auth.ToString().Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase);
    var parts = token.Split('.');
    if (parts.Length < 2) return null;

    try
    {
        var payload = parts[1];
        // Añadir padding Base64 si falta
        payload += (payload.Length % 4) switch { 2 => "==", 3 => "=", _ => "" };
        var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        foreach (var claim in new[] { "preferred_username", "email", "upn" })
            if (root.TryGetProperty(claim, out var val) && val.ValueKind == JsonValueKind.String)
                return val.GetString();
    }
    catch { /* Ignorar tokens malformados */ }

    return null;
}
```

> **IMPORTANTE de seguridad**: No confiar en el JWT sin verificarlo criptográficamente para decisiones de acceso críticas. Para autorización, usar Microsoft Graph o validar el token con la librería MSAL.
