using System.Text.Json;
using Bancos.Mcp.Tools;
using Microsoft.Extensions.Options;

namespace Bancos.Mcp.Protocol;

public static class McpHandler
{
    private const string DefaultProtocolVersion = "2025-06-18";
    private static readonly HashSet<string> CompatibleProtocolVersions = ["2024-11-05", DefaultProtocolVersion];

    public static IResult GetHealth() => TypedResults.Ok(new { status = "ready" });

    public static async Task<IResult> HandleAsync(
        HttpRequest request,
        ToolRegistry registry,
        IOptions<McpOptions> options,
        CancellationToken cancellationToken)
    {
        JsonElement body;
        try
        {
            using var document = await JsonDocument.ParseAsync(request.Body, cancellationToken: cancellationToken);
            body = NormalizeBatch(document.RootElement).Clone();
        }
        catch (JsonException)
        {
            return JsonRpcError(null, -32700, "Parse error");
        }
        catch (InvalidOperationException exception)
        {
            return JsonRpcError(null, -32600, exception.Message);
        }

        if (body.ValueKind != JsonValueKind.Object ||
            !body.TryGetProperty("jsonrpc", out var version) || version.GetString() != "2.0")
            return JsonRpcError(null, -32600, "Invalid Request");

        var hasId = body.TryGetProperty("id", out var id);
        if (!body.TryGetProperty("method", out var methodElement) || methodElement.ValueKind != JsonValueKind.String)
            return TypedResults.StatusCode(StatusCodes.Status202Accepted);

        var method = methodElement.GetString()!;
        if (method.StartsWith("notifications/", StringComparison.Ordinal))
            return TypedResults.StatusCode(StatusCodes.Status202Accepted);

        if (!hasId)
            return TypedResults.StatusCode(StatusCodes.Status202Accepted);

        var parameters = body.TryGetProperty("params", out var paramsElement)
            ? paramsElement
            : JsonDocument.Parse("{}").RootElement.Clone();

        return method switch
        {
            "initialize" => Initialize(id, parameters, options.Value),
            "tools/list" => JsonRpcResult(id, new { tools = registry.List() }),
            "tools/call" => await CallToolAsync(id, parameters, registry, cancellationToken),
            _ => JsonRpcError(id, -32601, "Method not found")
        };
    }

    private static IResult Initialize(JsonElement id, JsonElement parameters, McpOptions options)
    {
        var requestedVersion = parameters.TryGetProperty("protocolVersion", out var protocolVersion)
            ? protocolVersion.GetString()
            : null;
        var negotiatedVersion = requestedVersion is not null && CompatibleProtocolVersions.Contains(requestedVersion)
            ? requestedVersion
            : DefaultProtocolVersion;

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
            return JsonRpcResult(id, await tool.ExecuteAsync(arguments, cancellationToken));
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

    private static JsonElement NormalizeBatch(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Array)
            return root;

        if (root.GetArrayLength() != 1)
            throw new InvalidOperationException("Only a single JSON-RPC message is supported per batch.");

        var first = root[0];
        if (first.ValueKind != JsonValueKind.Object ||
            !first.TryGetProperty("jsonrpc", out var version) || version.GetString() != "2.0")
            throw new InvalidOperationException("Invalid Request");

        var hasMethod = first.TryGetProperty("method", out _);
        var hasId = first.TryGetProperty("id", out var id);
        if (!hasMethod && !hasId)
            return JsonDocument.Parse("""{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-06-18","clientInfo":{"name":"copilot-studio","version":"1.0.0"},"capabilities":{}}}""").RootElement.Clone();

        if (!hasMethod)
            return first;

        return first;
    }

    private static readonly JsonSerializerOptions CamelCase = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private static IResult JsonRpcResult(JsonElement id, object result) =>
        TypedResults.Json(new { jsonrpc = "2.0", id, result }, CamelCase);

    private static IResult JsonRpcError(JsonElement? id, int code, string message) =>
        TypedResults.Json(new { jsonrpc = "2.0", id, error = new JsonRpcError(code, message) }, CamelCase);
}
