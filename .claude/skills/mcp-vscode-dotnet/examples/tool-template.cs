using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace CompanyMcp.Tools.Features.Example;

[McpServerToolType]
public static class ExampleTool
{
    [McpServerTool(
        Name = "domain_action",
        Title = "Domain action",
        ReadOnly = false,
        Destructive = false,
        Idempotent = false,
        OpenWorld = true,
        UseStructuredContent = true)]
    [Description(
        "Previews or executes one bounded domain workflow. " +
        "Explain here when to use it and when another tool is more appropriate.")]
    public static async Task<DomainActionResult> ExecuteAsync(
        [Description("Concrete domain input and accepted format.")] string input,
        IDomainAction service,
        [Description("Set true to execute; false returns preview only.")] bool apply = false,
        CancellationToken ct = default)
    {
        input = input.Trim();
        if (input.Length is < 1 or > 200)
            throw new McpException("input must contain between 1 and 200 characters");

        if (!apply)
        {
            return new(
                Preview: true,
                Applied: false,
                Message: "Preview only. Call again with apply=true to execute.",
                Input: input,
                ResultId: null);
        }

        var id = await service.ExecuteAsync(input, ct);
        return new(false, true, "Action applied.", input, id);
    }
}

public sealed record DomainActionResult(
    bool Preview,
    bool Applied,
    string Message,
    string Input,
    string? ResultId);

public interface IDomainAction
{
    Task<string> ExecuteAsync(string input, CancellationToken ct);
}

// Register the implementation in CompanyMcpCatalog.AddDomainServices so both hosts share it.
