# MCP para ChatGPT — Implementación ASP.NET Core (.NET 10 / C# 14)

## Estructura de Archivos

```
Features/Mcp/
├── IMcpToolProvider.cs        ← Interfaz + records
├── McpToolRegistry.cs         ← Registro DI-based
├── McpJsonRpc.cs              ← Dispatch JSON-RPC 2.0 con headers spec
├── McpHandler.cs              ← Streamable HTTP (POST /mcp, GET /mcp, DELETE /mcp)
├── McpSession.cs              ← Estado de sesión (IMemoryCache)
├── McpArgs.cs                 ← Helpers compartidos
├── McpJson.cs                 ← JsonSerializerOptions compartidas
└── McpAuthMiddleware.cs       ← Validación API key / OAuth
Extensions/
└── McpEndpointExtensions.cs   ← Registro de rutas
```

---

## McpAuthMiddleware — Autenticación

```csharp
public static class McpAuth
{
    public static IResult? Validate(HttpRequest req, IOptions<McpOptions> opts)
    {
        // API Key validation
        var auth = req.Headers.Authorization.ToString();
        if (string.IsNullOrEmpty(auth))
            return Results.Json(
                new { error = "Authorization header required" },
                statusCode: 401);

        var token = auth.Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase).Trim();
        if (token != opts.Value.ApiKey)
            return Results.Json(
                new { error = "Invalid API key" },
                statusCode: 401);

        return null; // Auth OK
    }
}
```

**Configuración:**
```json
// appsettings.json
{
  "Mcp": {
    "ApiKey": "sk-your-secret-key-here",
    "AllowedOrigins": ["https://chatgpt.com", "https://copilot.microsoft.com"]
  }
}

// McpOptions.cs
public sealed class McpOptions
{
    public string ApiKey { get; set; } = "";
    public string[] AllowedOrigins { get; set; } = [];
}
```

**Variable de entorno para deploy:**
```
Mcp__ApiKey=${{ secrets.MCP_API_KEY }}
Mcp__AllowedOrigins__0=https://chatgpt.com
Mcp__AllowedOrigins__1=https://copilot.microsoft.com
```

---

## McpJsonRpc — Dispatch con Headers Spec 2025-06-18

```csharp
public static class McpJsonRpc
{
    private const string ServerName = "voicebot-purchase";
    private const string ServerVersion = "1.0.0";
    private static readonly string[] CompatibleVersions = ["2024-11-05", "2025-06-18"];
    private const string DefaultVersion = "2025-06-18";

    public static async Task<(object? Response, string? SessionId)> DispatchAsync(
        JsonElement body, McpToolRegistry registry,
        ILlmAuditService audit, IMemoryCache cache,
        string? incomingSessionId, CancellationToken ct)
    {
        var id = body.TryGetProperty("id", out var idEl) ? (JsonElement?)idEl : null;
        var method = body.TryGetProperty("method", out var methEl) ? methEl.GetString() : null;
        body.TryGetProperty("params", out var paramsEl);

        if (method is null && id.HasValue)
            return (null, null);

        return method switch
        {
            "initialize" => HandleInitialize(id, paramsEl, cache),
            "notifications/initialized" => (null, null), // 202
            "tools/list" => (JsonRpc(id, new { tools = registry.GetAllDefinitions() }), null),
            "tools/call" => (await HandleToolsCallAsync(id, paramsEl, registry, audit, ct), null),
            _ => (JsonRpcError(id, -32601, $"Method not found: {method}"), null),
        };
    }

    private static (object Response, string SessionId) HandleInitialize(
        JsonElement? id, JsonElement @params, IMemoryCache cache)
    {
        var clientVersion = @params.ValueKind != JsonValueKind.Undefined
            && @params.TryGetProperty("protocolVersion", out var cv)
            ? cv.GetString() : DefaultVersion;

        var version = CompatibleVersions.Contains(clientVersion) ? clientVersion : DefaultVersion;
        var sessionId = Guid.NewGuid().ToString("N");

        // Guardar sesión en cache
        cache.Set($"mcp:{sessionId}", new McpSession(), TimeSpan.FromMinutes(30));

        var result = JsonRpc(id, new
        {
            protocolVersion = version,
            capabilities = new { tools = new { listChanged = false } },
            serverInfo = new { name = ServerName, version = ServerVersion },
        });

        return (result, sessionId);
    }
}
```

---

## McpHandler — Endpoint con Headers

```csharp
public static class McpHandler
{
    public static async Task<IResult> HandlePostAsync(
        HttpContext ctx, McpToolRegistry registry,
        ILlmAuditService audit, IMemoryCache cache,
        IOptions<McpOptions> opts, CancellationToken ct)
    {
        // 1. Auth
        var authError = McpAuth.Validate(ctx.Request, opts);
        if (authError is not null) return authError;

        // 2. Origin validation
        var origin = ctx.Request.Headers.Origin.ToString();
        if (!string.IsNullOrEmpty(origin) && !opts.Value.AllowedOrigins.Contains(origin))
            return Results.StatusCode(403);

        // 3. Parse body
        JsonElement body;
        try
        {
            using var doc = await JsonDocument.ParseAsync(ctx.Request.Body, cancellationToken: ct);
            body = doc.RootElement.Clone();
        }
        catch (JsonException ex)
        {
            return Results.Json(McpJsonRpc.JsonRpcError(null, -32700, $"Parse error: {ex.Message}"));
        }

        // 4. Validate MCP-Protocol-Version (post-initialize)
        var method = body.TryGetProperty("method", out var m) ? m.GetString() : null;
        var incomingSessionId = ctx.Request.Headers["Mcp-Session-Id"].FirstOrDefault();

        if (method != "initialize")
        {
            var protoVersion = ctx.Request.Headers["MCP-Protocol-Version"].FirstOrDefault();
            if (protoVersion is not null && !McpJsonRpc.IsVersionSupported(protoVersion))
                return Results.StatusCode(400);
        }

        // 5. Dispatch
        var (response, newSessionId) = await McpJsonRpc.DispatchAsync(
            body, registry, audit, cache, incomingSessionId, ct);

        if (response is null)
            return Results.StatusCode(202);

        // 6. Set Mcp-Session-Id on initialize response
        if (newSessionId is not null)
            ctx.Response.Headers["Mcp-Session-Id"] = newSessionId;

        return Results.Json(response, McpJson.CamelCase);
    }

    public static IResult HandleDelete(HttpContext ctx, IMemoryCache cache)
    {
        var sessionId = ctx.Request.Headers["Mcp-Session-Id"].FirstOrDefault();
        if (string.IsNullOrEmpty(sessionId))
            return Results.NotFound();

        cache.Remove($"mcp:{sessionId}");
        return Results.Ok();
    }

    public static IResult HealthCheck() => TypedResults.Ok("MCP ready");
}
```

---

## McpEndpointExtensions — Rutas

```csharp
public static class McpEndpointExtensions
{
    public static IEndpointRouteBuilder MapMcpEndpoints(this IEndpointRouteBuilder app)
    {
        var mcp = app.MapGroup("/mcp")
            .AllowAnonymous()        // Auth se maneja en McpAuth, no en middleware
            .ExcludeFromDescription();

        mcp.MapGet("/", McpHandler.HealthCheck);
        mcp.MapPost("/", McpHandler.HandlePostAsync);
        mcp.MapDelete("/", McpHandler.HandleDelete);

        return app;
    }
}
```

---

## Tool con outputSchema y structuredContent

```csharp
public sealed class CustomerTool(ICustomerService svc) : IMcpToolProvider
{
    public IReadOnlyList<McpToolDefinition> GetDefinitions() =>
    [
        new("get_customer",
            "Busca un cliente por teléfono. Retorna nombre, dirección y datos de contacto.",
            new {
                type = "object",
                properties = new {
                    phone = new { type = "string", description = "Teléfono 8 dígitos" }
                },
                required = new[] { "phone" }
            },
            OutputSchema: new {
                type = "object",
                properties = new {
                    customerId = new { type = "integer" },
                    name = new { type = "string" },
                    phone = new { type = "string" },
                    email = new { type = "string" }
                },
                required = new[] { "customerId", "name", "phone" }
            })
    ];

    public async Task<McpToolResult> ExecuteAsync(string toolName, JsonElement args, CancellationToken ct)
    {
        var phone = args.GetProperty("phone").GetString()!;
        var customer = await svc.FindByPhoneAsync(phone, ct);

        var structured = new {
            customerId = customer.Id,
            name = customer.Name,
            phone = customer.Phone,
            email = customer.Email
        };

        var text = $"customerId: {customer.Id}\nname: {customer.Name}\nphone: {customer.Phone}";

        return new McpToolResult(text, structured);
    }
}
```

---

## Patrones .NET 10 / C# 14 Aplicados

| Patrón | Uso |
|--------|-----|
| Primary constructors | Tools, Registry, Services |
| `sealed` en todas las clases | Tools, McpSession, McpOptions |
| `IOptions<T>` + `ValidateOnStart()` | McpOptions (ApiKey, AllowedOrigins) |
| `IMemoryCache` | Session state con TTL 30min |
| `CancellationToken ct` | Todos los métodos async |
| Collection expressions `[..]` | Registry, tool definitions |
| Switch expressions | JSON-RPC method routing |
| `TypedResults` / `Results.Json` | Respuestas HTTP |
