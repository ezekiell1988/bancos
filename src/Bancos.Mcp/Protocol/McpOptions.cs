using System.ComponentModel.DataAnnotations;

namespace Bancos.Mcp.Protocol;

public sealed class McpOptions
{
    public const string Section = "Mcp";

    [Required, MinLength(1)]
    public string ServerName { get; init; } = "bancos-mcp";

    [Required, MinLength(1)]
    public string ServerVersion { get; init; } = "1.0.0";
}
