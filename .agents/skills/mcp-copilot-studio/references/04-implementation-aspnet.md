# MCP — Implementación ASP.NET Core Minimal APIs (.NET 10 / C# 14)

Implementación primaria ⭐ — Sin prefijos de ruta, más control directo sobre el pipeline HTTP.

## Estructura de Archivos

```
mcp-server/
├── mcp-server.csproj
├── Program.cs               ← Bootstrap + middleware
├── McpModule.cs             ← Module: DI + endpoint registration
├── McpHandler.cs            ← JSON-RPC routing (static)
├── Options/
│   └── McpOptions.cs        ← IOptions config
├── Models/
│   └── McpModels.cs         ← Records de protocolo
└── Tools/
    ├── IMcpTool.cs           ← Interfaz
    ├── ToolRegistry.cs       ← Registro DI-based
    └── BuscarClienteTool.cs  ← Tool de ejemplo
```

---

## `mcp-server.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <LangVersion>14</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>
```

---

## `Models/McpModels.cs`

```csharp
namespace McpServer.Models;

public sealed record McpTool(
    string Name,
    string Title,
    string Description,
    object InputSchema);

public sealed record InputSchema(
    string Type,
    Dictionary<string, object> Properties,
    string[]? Required = null);

// Resultado siempre content[0].text
public sealed record McpContent(string Type, string Text)
{
    public static McpContent Text(string text) => new("text", text);
}
```

---

## `Options/McpOptions.cs`

```csharp
using System.ComponentModel.DataAnnotations;

namespace McpServer.Options;

public sealed class McpOptions
{
    [Required] public string ServerName    { get; set; } = "mi-mcp-server";
    [Required] public string ServerVersion { get; set; } = "1.0.0";
}
```

---

## `Tools/IMcpTool.cs`

```csharp
using McpServer.Models;

namespace McpServer.Tools;

public interface IMcpTool
{
    bool IsPrivate { get; }
    McpTool GetDefinition();
    Task<string> ExecuteAsync(JsonElement arguments, string? userEmail, CancellationToken ct);
}
```

---

## `Tools/ToolRegistry.cs`

```csharp
using McpServer.Models;

namespace McpServer.Tools;

public sealed class ToolRegistry(IEnumerable<IMcpTool> tools)
{
    private readonly IReadOnlyList<IMcpTool> _tools = [..tools];

    public IEnumerable<McpTool> GetAccessibleTools(string? email) =>
        _tools
            .Where(t => !t.IsPrivate || email is not null)
            .Select(t => t.GetDefinition());

    public IMcpTool? Find(string name) =>
        _tools.FirstOrDefault(t => t.GetDefinition().Name == name);
}
```

---

## `Tools/BuscarClienteTool.cs` (tool de ejemplo)

```csharp
using McpServer.Models;

namespace McpServer.Tools;

public sealed class BuscarClienteTool(IHttpClientFactory httpFactory) : IMcpTool
{
    public bool IsPrivate => false;   // Pública

    public McpTool GetDefinition() => new(
        Name:        "buscar_cliente",
        Title:       "Buscar Cliente",
        Description: "Busca un cliente en el CRM por nombre o email. Úsala cuando el usuario pregunte por un cliente.",
        InputSchema: new InputSchema(
            Type:       "object",
            Properties: new()
            {
                ["query"] = new { type = "string", description = "Nombre, email o teléfono del cliente" },
                ["limite"] = new { type = "integer", description = "Máx resultados (default 5)", @default = 5, minimum = 1, maximum = 20 }
            },
            Required: ["query"]));

    public async Task<string> ExecuteAsync(JsonElement args, string? userEmail, CancellationToken ct)
    {
        var query  = args.TryGetProperty("query",  out var q) ? q.GetString() : null
                     ?? throw new ArgumentException("query es requerido");
        var limite = args.TryGetProperty("limite", out var l) ? l.GetInt32() : 5;

        // Llamada HTTP real usando HttpClient con resiliencia
        var client = httpFactory.CreateClient("CrmApi");
        var result = await client.GetStringAsync($"/api/clientes?q={Uri.EscapeDataString(query)}&top={limite}", ct);
        return result;
    }
}
```

---

## `McpHandler.cs`

```csharp
using System.Text;
using McpServer.Models;
using McpServer.Options;
using McpServer.Tools;
using Microsoft.Extensions.Options;

namespace McpServer;

public static class McpHandler
{
    private static readonly string[] CompatibleVersions = ["2024-11-05", "2025-06-18"];
    private const string DefaultVersion = "2025-06-18";

    public static IResult HealthCheck() => TypedResults.Ok("MCP ready");

    public static async Task<IResult> HandleAsync(
        HttpRequest httpReq,
        ToolRegistry registry,
        IOptions<McpOptions> opts,
        CancellationToken ct)
    {
        JsonElement body;
        try
        {
            using var doc = await JsonDocument.ParseAsync(httpReq.Body, cancellationToken: ct);
            body = NormalizeBatch(doc.RootElement).Clone();
        }
        catch (JsonException ex)
        {
            return JsonRpcError(null, -32700, $"Parse error: {ex.Message}");
        }

        var id     = body.TryGetProperty("id",     out var idEl)  ? (JsonElement?)idEl  : null;
        var method = body.TryGetProperty("method", out var methEl) ? methEl.GetString() : null;
        body.TryGetProperty("params", out var paramsEl);

        // Respuesta del cliente (id sin method) → 202
        if (method is null && id.HasValue)
            return Results.StatusCode(202);

        var userEmail = ExtractUserEmail(httpReq);

        return method switch
        {
            "initialize"                => HandleInitialize(id, paramsEl, opts.Value),
            "notifications/initialized" => Results.StatusCode(202),
            "tools/list"                => HandleToolsList(id, registry, userEmail),
            "tools/call"                => await HandleToolsCallAsync(id, paramsEl, registry, userEmail, ct),
            _                           => JsonRpcError(id, -32601, $"Method not found: {method}")
        };
    }

    // ── Handlers ─────────────────────────────────────────────────────────────

    private static IResult HandleInitialize(JsonElement? id, JsonElement @params, McpOptions opts)
    {
        var clientVersion = @params.ValueKind != JsonValueKind.Undefined
            && @params.TryGetProperty("protocolVersion", out var cv)
            ? cv.GetString() : DefaultVersion;

        var version = CompatibleVersions.Contains(clientVersion) ? clientVersion : DefaultVersion;

        return JsonRpc(id, new
        {
            protocolVersion = version,
            capabilities    = new { tools = new { listChanged = false } },
            serverInfo      = new { name = opts.ServerName, version = opts.ServerVersion }
        });
    }

    private static IResult HandleToolsList(JsonElement? id, ToolRegistry registry, string? email) =>
        JsonRpc(id, new { tools = registry.GetAccessibleTools(email) });

    private static async Task<IResult> HandleToolsCallAsync(
        JsonElement? id, JsonElement @params, ToolRegistry registry, string? email, CancellationToken ct)
    {
        var toolName = @params.TryGetProperty("name", out var n) ? n.GetString() : null;
        if (toolName is null)
            return JsonRpcError(id, -32602, "params.name requerido");

        var tool = registry.Find(toolName);
        if (tool is null)
            return JsonRpcError(id, -32602, $"Tool desconocida: {toolName}");

        if (tool.IsPrivate && email is null)
            return JsonRpcError(id, -32603, "Autenticación requerida para esta tool");

        @params.TryGetProperty("arguments", out var argsEl);

        try
        {
            var text = await tool.ExecuteAsync(argsEl, email, ct);
            return JsonRpc(id, new { content = new[] { McpContent.Text(text) } });
        }
        catch (ArgumentException ex)
        {
            return JsonRpcError(id, -32602, ex.Message);
        }
        catch (Exception ex)
        {
            return JsonRpcError(id, -32603, $"Internal error: {ex.Message}");
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static IResult JsonRpc(JsonElement? id, object result) =>
        Results.Json(new { jsonrpc = "2.0", id, result }, contentType: "application/json");

    private static IResult JsonRpcError(JsonElement? id, int code, string message) =>
        Results.Json(new { jsonrpc = "2.0", id, error = new { code, message } }, contentType: "application/json");

    private static string? ExtractUserEmail(HttpRequest req)
    {
        if (req.Headers.TryGetValue("X-MS-CLIENT-PRINCIPAL-NAME", out var name))
            return name.ToString();

        if (!req.Headers.TryGetValue("Authorization", out var auth))
            return null;

        var token = auth.ToString().Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase);
        var parts = token.Split('.');
        if (parts.Length < 2) return null;

        try
        {
            var payload = parts[1];
            payload += (payload.Length % 4) switch { 2 => "==", 3 => "=", _ => "" };
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            foreach (var claim in new[] { "preferred_username", "email", "upn" })
                if (root.TryGetProperty(claim, out var val) && val.ValueKind == JsonValueKind.String)
                    return val.GetString();
        }
        catch { /* Token malformado */ }

        return null;
    }

    internal static JsonElement NormalizeBatch(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Array)
            return root;

        if (root.GetArrayLength() != 1)
            throw new InvalidOperationException("Batch multi-elemento no soportado");

        var first     = root[0];
        var hasMethod = first.TryGetProperty("method", out _);
        var hasId     = first.TryGetProperty("id",     out var idProp);

        if (!hasMethod && !hasId)
            return JsonDocument.Parse("""
                {"jsonrpc":"2.0","id":1,"method":"initialize",
                 "params":{"protocolVersion":"2025-06-18",
                            "clientInfo":{"name":"copilot-studio","version":"1.0.0"}}}
                """).RootElement;

        if (!hasMethod)
            return JsonDocument.Parse($$"""
                {"jsonrpc":"2.0","id":{{idProp.GetRawText()}},"method":"tools/list","params":{}}
                """).RootElement;

        return first;
    }
}
```

---

## `McpModule.cs`

```csharp
using McpServer.Options;
using McpServer.Tools;

namespace McpServer;

public static class McpModule
{
    public static IServiceCollection AddMcpServer(this IServiceCollection services)
    {
        // Options con validación al arranque
        services.AddOptions<McpOptions>()
                .BindConfiguration("Mcp")
                .ValidateDataAnnotations()
                .ValidateOnStart();

        // Registro de tools (añadir aquí cada nueva tool)
        services.AddSingleton<IMcpTool, BuscarClienteTool>();
        // services.AddSingleton<IMcpTool, OtraTool>();

        services.AddSingleton<ToolRegistry>();

        // HttpClient con resiliencia estándar para tools que hacen llamadas externas
        services.AddHttpClient("CrmApi", c =>
        {
            c.BaseAddress = new Uri("https://mi-crm.example.com");
        }).AddStandardResilienceHandler();

        return services;
    }

    public static IEndpointRouteBuilder MapMcpEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet( "/{**slug}", McpHandler.HealthCheck).ExcludeFromDescription();
        app.MapPost("/{**slug}", McpHandler.HandleAsync) .ExcludeFromDescription();
        return app;
    }
}
```

---

## `Program.cs`

```csharp
using McpServer;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddMcpServer();

var app = builder.Build();

app.UseExceptionHandler();
app.UseHttpsRedirection();

app.MapMcpEndpoints();

app.Run();
```

---

## `appsettings.json`

```json
{
  "Mcp": {
    "ServerName": "mi-mcp-server",
    "ServerVersion": "1.0.0"
  }
}
```

---

## Patrones .NET 10 / C# 14 Aplicados

| Patrón | Cómo se aplica en MCP |
|--------|-----------------------|
| `net10.0` + `LangVersion 14` | `mcp-server.csproj` |
| Records para modelos | `McpTool`, `McpContent`, `InputSchema` |
| Module pattern | `McpModule` con `AddMcpServer()` + `MapMcpEndpoints()` |
| `IOptions<T>` + `ValidateOnStart()` | `McpOptions` para nombre/versión del servidor |
| `AddStandardResilienceHandler()` | HttpClient de tools que llaman APIs externas |
| `AddProblemDetails()` + `UseExceptionHandler()` | Errores inesperados en `Program.cs` |
| `TypedResults.Ok()` | Respuesta GET health-check |
| `Results.Json()` | Respuestas JSON-RPC (requieren JSON puro) |
| `Results.StatusCode(202)` | Notificaciones y respuestas del cliente |
| C# 14 collection expressions `[..tools]` | `ToolRegistry` constructor |
| Switch expressions | Router JSON-RPC en `McpHandler.HandleAsync` |
