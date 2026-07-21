using System.Text.Json;
using Bancos.Mcp.Protocol;

namespace Bancos.Mcp.Tools;

public interface IMcpTool
{
    McpToolDefinition Definition { get; }
    ValueTask<McpToolResult> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken);
}
