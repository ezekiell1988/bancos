using System.Text.Json;
using Bancos.Mcp.Protocol;

namespace Bancos.Mcp.Tools;

public sealed class StatusTool : IMcpTool
{
    public McpToolDefinition Definition { get; } = new(
        Name: "health_status",
        Title: "Estado del servidor Bancos MCP",
        Description: "Confirma que el servidor MCP de Bancos está disponible. No consulta datos financieros ni sistemas externos.",
        InputSchema: new { type = "object", properties = new { }, additionalProperties = false },
        OutputSchema: new
        {
            type = "object",
            properties = new { status = new { type = "string" } },
            required = new[] { "status" },
            additionalProperties = false
        });

    public ValueTask<McpToolResult> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken) =>
        ValueTask.FromResult(new McpToolResult([McpContent.FromText("Estado: disponible")], new { status = "available" }));
}
