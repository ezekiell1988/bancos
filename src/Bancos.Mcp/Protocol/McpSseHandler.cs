using System.Collections.Concurrent;
using System.Text.Json;
using Bancos.Mcp.Tools;
using Microsoft.Extensions.Options;

namespace Bancos.Mcp.Protocol;

public static class McpSseHandler
{
    private static readonly ConcurrentDictionary<string, SseSession> Sessions = new();

    public static async Task HandleSseAsync(HttpContext context, CancellationToken cancellationToken)
    {
        var sessionId = Guid.NewGuid().ToString("N");
        var session = new SseSession();
        Sessions[sessionId] = session;

        context.Response.ContentType = "text/event-stream";
        context.Response.Headers.CacheControl = "no-cache";
        context.Response.Headers.Connection = "keep-alive";

        var baseUrl = $"{context.Request.Scheme}://{context.Request.Host}";
        await WriteSseEvent(context.Response, "endpoint", $"{baseUrl}/mcp/sse/message?sessionId={sessionId}", cancellationToken);

        try
        {
            await foreach (var message in session.Messages.Reader.ReadAllAsync(cancellationToken))
            {
                await WriteSseEvent(context.Response, "message", message, cancellationToken);
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            Sessions.TryRemove(sessionId, out _);
        }
    }

    public static async Task<IResult> HandleMessageAsync(
        HttpRequest request,
        ToolRegistry registry,
        IOptions<McpOptions> options,
        string sessionId,
        CancellationToken cancellationToken)
    {
        if (!Sessions.TryGetValue(sessionId, out var session))
            return TypedResults.NotFound("Session not found");

        JsonElement body;
        try
        {
            using var document = await JsonDocument.ParseAsync(request.Body, cancellationToken: cancellationToken);
            body = document.RootElement.Clone();
        }
        catch (JsonException)
        {
            return TypedResults.BadRequest("Parse error");
        }

        if (body.ValueKind != JsonValueKind.Object ||
            !body.TryGetProperty("jsonrpc", out var version) || version.GetString() != "2.0")
        {
            await session.SendAsync(JsonSerializer.Serialize(new { jsonrpc = "2.0", id = (object?)null, error = new { code = -32600, message = "Invalid Request" } }));
            return TypedResults.Ok("accepted");
        }

        var hasId = body.TryGetProperty("id", out var id);
        if (!body.TryGetProperty("method", out var methodElement) || methodElement.ValueKind != JsonValueKind.String)
            return TypedResults.Ok("accepted");

        var method = methodElement.GetString()!;
        if (method.StartsWith("notifications/", StringComparison.Ordinal))
            return TypedResults.Ok("accepted");

        if (!hasId)
            return TypedResults.Ok("accepted");

        var parameters = body.TryGetProperty("params", out var paramsElement)
            ? paramsElement
            : JsonDocument.Parse("{}").RootElement.Clone();

        var result = method switch
        {
            "initialize" => InitializeResponse(id, parameters, options.Value),
            "tools/list" => JsonRpcResponse(id, new { tools = registry.List() }),
            "tools/call" => await CallToolResponse(id, parameters, registry, cancellationToken),
            _ => JsonRpcErrorResponse(id, -32601, "Method not found")
        };

        await session.SendAsync(result);
        return TypedResults.Ok("accepted");
    }

    private static string InitializeResponse(JsonElement id, JsonElement parameters, McpOptions options)
    {
        var requestedVersion = parameters.TryGetProperty("protocolVersion", out var pv) ? pv.GetString() : null;
        var negotiated = requestedVersion is "2024-11-05" or "2025-06-18" ? requestedVersion : "2024-11-05";
        return JsonRpcResponse(id, new
        {
            protocolVersion = negotiated,
            capabilities = new { tools = new { listChanged = false } },
            serverInfo = new { name = options.ServerName, version = options.ServerVersion }
        });
    }

    private static async Task<string> CallToolResponse(JsonElement id, JsonElement parameters, ToolRegistry registry, CancellationToken ct)
    {
        if (!parameters.TryGetProperty("name", out var nameEl) || nameEl.ValueKind != JsonValueKind.String)
            return JsonRpcErrorResponse(id, -32602, "Tool name is required");

        if (!registry.TryGet(nameEl.GetString()!, out var tool) || tool is null)
            return JsonRpcErrorResponse(id, -32602, "Unknown tool");

        var arguments = parameters.TryGetProperty("arguments", out var argEl) ? argEl : JsonDocument.Parse("{}").RootElement.Clone();

        try
        {
            return JsonRpcResponse(id, await tool.ExecuteAsync(arguments, ct));
        }
        catch (ArgumentException ex) { return JsonRpcErrorResponse(id, -32602, ex.Message); }
        catch (InvalidDataException ex) { return JsonRpcErrorResponse(id, -32602, ex.Message); }
        catch { return JsonRpcErrorResponse(id, -32603, "Internal error"); }
    }

    private static readonly JsonSerializerOptions CamelCase = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private static string JsonRpcResponse(JsonElement id, object result) =>
        JsonSerializer.Serialize(new { jsonrpc = "2.0", id, result }, CamelCase);

    private static string JsonRpcErrorResponse(JsonElement id, int code, string message) =>
        JsonSerializer.Serialize(new { jsonrpc = "2.0", id, error = new { code, message } }, CamelCase);

    private static async Task WriteSseEvent(HttpResponse response, string eventType, string data, CancellationToken ct)
    {
        await response.WriteAsync($"event: {eventType}\ndata: {data}\n\n", ct);
        await response.Body.FlushAsync(ct);
    }
}

internal sealed class SseSession
{
    public System.Threading.Channels.Channel<string> Messages { get; } =
        System.Threading.Channels.Channel.CreateUnbounded<string>();

    public async Task SendAsync(string message) =>
        await Messages.Writer.WriteAsync(message);
}
