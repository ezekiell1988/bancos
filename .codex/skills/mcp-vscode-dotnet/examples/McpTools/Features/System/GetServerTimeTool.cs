using System.ComponentModel;
using ModelContextProtocol.Server;

namespace CompanyMcp.Tools.Features.System;

[McpServerToolType]
public static class GetServerTimeTool
{
    [McpServerTool(
        Name = "server_time",
        Title = "Server time",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false,
        UseStructuredContent = true)]
    [Description("Returns the server's current UTC time. Use it when an exact server timestamp is required.")]
    public static ServerTimeResult GetServerTime() =>
        new(DateTimeOffset.UtcNow);
}

public sealed record ServerTimeResult(DateTimeOffset Utc);
