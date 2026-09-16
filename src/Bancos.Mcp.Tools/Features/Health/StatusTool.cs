using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Bancos.Mcp.Features.Health;

[McpServerToolType]
public static class StatusTool
{
    [McpServerTool(
        Name = "health_status",
        Title = "Estado del servidor Bancos MCP",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false,
        UseStructuredContent = true)]
    [Description("Confirma que el servidor MCP de Bancos está disponible. No consulta datos financieros ni sistemas externos.")]
    public static HealthStatusResult GetStatus() => new("available");
}

public sealed record HealthStatusResult(string Status);
