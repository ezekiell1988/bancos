using ModelContextProtocol.Client;
using Xunit;

namespace Bancos.Mcp.Tests;

public sealed class StdioSmokeTests
{
    private static string RepoRoot => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    [Fact]
    public async Task Stdio_host_exposes_the_full_catalog_and_answers_health_status()
    {
        var stdioProject = Path.Combine(RepoRoot, "src", "Bancos.Mcp.Stdio", "Bancos.Mcp.Stdio.csproj");
        Assert.True(File.Exists(stdioProject), $"No se encontró el proyecto stdio en '{stdioProject}'.");

        var transport = new StdioClientTransport(new StdioClientTransportOptions
        {
            Command = "dotnet",
            Arguments = ["run", "--project", stdioProject, "--no-launch-profile", "--tl:off", "--verbosity", "quiet"],
            WorkingDirectory = RepoRoot
        });

        await using var client = await McpClient.CreateAsync(transport);

        var tools = await client.ListToolsAsync();
        Assert.Contains(tools, tool => tool.Name == "health_status");
        Assert.Contains(tools, tool => tool.Name == "get_ledger_period");
        Assert.True(tools.Count >= 25, $"Se esperaban al menos 25 tools por stdio, se obtuvieron {tools.Count}.");

        var result = await client.CallToolAsync("health_status", new Dictionary<string, object?>());
        Assert.NotEqual(true, result.IsError);
        Assert.NotNull(result.StructuredContent);
    }
}
