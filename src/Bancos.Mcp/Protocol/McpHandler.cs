using System.Text.Json;
using Bancos.Mcp.Tools;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Bancos.Mcp.Protocol;

public static class McpHandler
{
    private const string DefaultProtocolVersion = "2025-06-18";
    private static readonly HashSet<string> CompatibleProtocolVersions = ["2024-11-05", DefaultProtocolVersion];

    public static IResult GetHealth() => TypedResults.Ok(new { status = "ready" });

    public static async Task<IResult> HandleAsync(
        HttpContext context,
        ToolRegistry registry,
        IMemoryCache cache,
        IOptions<McpOptions> options,
        CancellationToken cancellationToken)
    {
        if (!IsOriginAllowed(context.Request, options.Value))
            return TypedResults.StatusCode(StatusCodes.Status403Forbidden);

        JsonElement body;
        try
        {
            using var document = await JsonDocument.ParseAsync(context.Request.Body, cancellationToken: cancellationToken);
            body = document.RootElement.Clone();
        }
        catch (JsonException)
        {
            return JsonRpcError(null, -32700, "Parse error");
        }

        if (body.ValueKind != JsonValueKind.Object ||
            !body.TryGetProperty("jsonrpc", out var version) || version.GetString() != "2.0")
            return JsonRpcError(null, -32600, "Invalid Request");

        var hasId = body.TryGetProperty("id", out var id);
        if (!body.TryGetProperty("method", out var methodElement) || methodElement.ValueKind != JsonValueKind.String)
            return TypedResults.StatusCode(StatusCodes.Status202Accepted);

        var method = methodElement.GetString()!;
        if (method != "initialize" && !HasValidSessionAndVersion(context.Request, cache))
            return TypedResults.StatusCode(StatusCodes.Status400BadRequest);

        if (method.StartsWith("notifications/", StringComparison.Ordinal) || !hasId)
            return TypedResults.StatusCode(StatusCodes.Status202Accepted);

        var parameters = body.TryGetProperty("params", out var paramsElement)
            ? paramsElement
            : JsonDocument.Parse("{}").RootElement.Clone();

        return method switch
        {
            "initialize" => Initialize(context.Response, id, parameters, options.Value, cache),
            "tools/list" => JsonRpcResult(id, new { tools = registry.List() }),
            "tools/call" => await CallToolAsync(id, parameters, registry, cancellationToken),
            _ => JsonRpcError(id, -32601, "Method not found")
        };
    }

    public static Task<IResult> HandleDeleteAsync(HttpRequest request, IMemoryCache cache, IOptions<McpOptions> options)
    {
        if (!IsOriginAllowed(request, options.Value))
            return Task.FromResult<IResult>(TypedResults.StatusCode(StatusCodes.Status403Forbidden));

        var sessionId = request.Headers["Mcp-Session-Id"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(sessionId) || !cache.TryGetValue(SessionKey(sessionId), out _))
            return Task.FromResult<IResult>(TypedResults.NotFound());

        cache.Remove(SessionKey(sessionId));
        return Task.FromResult<IResult>(TypedResults.Ok());
    }

    private static bool IsOriginAllowed(HttpRequest request, McpOptions options)
    {
        var origin = request.Headers.Origin.FirstOrDefault();
        return string.IsNullOrWhiteSpace(origin) || options.AllowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase);
    }

    private static bool HasValidSessionAndVersion(HttpRequest request, IMemoryCache cache)
    {
        var sessionId = request.Headers["Mcp-Session-Id"].FirstOrDefault();
        var protocolVersion = request.Headers["MCP-Protocol-Version"].FirstOrDefault();
        return !string.IsNullOrWhiteSpace(sessionId)
            && cache.TryGetValue(SessionKey(sessionId), out _)
            && !string.IsNullOrWhiteSpace(protocolVersion)
            && CompatibleProtocolVersions.Contains(protocolVersion);
    }

    private static IResult Initialize(HttpResponse response, JsonElement id, JsonElement parameters, McpOptions options, IMemoryCache cache)
    {
        var requestedVersion = parameters.TryGetProperty("protocolVersion", out var protocolVersion)
            ? protocolVersion.GetString()
            : null;
        var negotiatedVersion = requestedVersion is not null && CompatibleProtocolVersions.Contains(requestedVersion)
            ? requestedVersion
            : DefaultProtocolVersion;
        var sessionId = Guid.NewGuid().ToString("N");

        cache.Set(SessionKey(sessionId), true, TimeSpan.FromMinutes(30));
        response.Headers["Mcp-Session-Id"] = sessionId;

        return JsonRpcResult(id, new
        {
            protocolVersion = negotiatedVersion,
            capabilities = new { tools = new { listChanged = false } },
            serverInfo = new { name = options.ServerName, version = options.ServerVersion }
        });
    }

    private static async Task<IResult> CallToolAsync(JsonElement id, JsonElement parameters, ToolRegistry registry, CancellationToken cancellationToken)
    {
        if (!parameters.TryGetProperty("name", out var nameElement) || nameElement.ValueKind != JsonValueKind.String)
            return JsonRpcError(id, -32602, "Tool name is required");

        if (!registry.TryGet(nameElement.GetString()!, out var tool) || tool is null)
            return JsonRpcError(id, -32602, "Unknown tool");

        var arguments = parameters.TryGetProperty("arguments", out var argumentElement)
            ? argumentElement
            : JsonDocument.Parse("{}").RootElement.Clone();

        try
        {
            var toolResult = await tool.ExecuteAsync(arguments, cancellationToken);
            return JsonRpcResult(id, new { content = toolResult.Content, structuredContent = toolResult.StructuredContent });
        }
        catch (ArgumentException exception)
        {
            return JsonRpcError(id, -32602, exception.Message);
        }
        catch (InvalidDataException exception)
        {
            return JsonRpcError(id, -32602, exception.Message);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return JsonRpcError(id, -32603, "Internal error");
        }
    }

    private static string SessionKey(string sessionId) => $"mcp-session:{sessionId}";

    private static readonly JsonSerializerOptions CamelCase = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private static IResult JsonRpcResult(JsonElement id, object result) =>
        TypedResults.Json(new { jsonrpc = "2.0", id, result }, CamelCase);

    private static IResult JsonRpcError(JsonElement? id, int code, string message) =>
        TypedResults.Json(new { jsonrpc = "2.0", id, error = new JsonRpcError(code, message) }, CamelCase);
}
