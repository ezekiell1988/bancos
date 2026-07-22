using System.Text.Json;

namespace Bancos.Mcp.Protocol;

public sealed record McpToolDefinition(string Name, string Title, string Description, object InputSchema);

public sealed record McpContent(string Type, string Text)
{
    public static McpContent FromText(string text) => new("text", text);
}

public sealed record McpToolResult(IReadOnlyList<McpContent> Content)
{
    public static McpToolResult Error(string message) => new([McpContent.FromText(message)]);
}

public sealed record JsonRpcError(int Code, string Message);

public sealed class JsonRpcRequest
{
    public required JsonElement Id { get; init; }
    public required string Method { get; init; }
    public required JsonElement Params { get; init; }
}
