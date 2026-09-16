using System.Net;
using System.Net.Http.Json;
using Bancos.Mcp.Features.TemplateDetection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using ModelContextProtocol.Client;
using Xunit;

namespace Bancos.Mcp.Tests;

public sealed class McpProtocolTests : IClassFixture<McpWebApplicationFactory>
{
    private readonly McpWebApplicationFactory factory;

    public McpProtocolTests(McpWebApplicationFactory factory) => this.factory = factory;

    [Fact]
    public async Task Tools_list_exposes_the_full_catalog_over_http()
    {
        await using var client = await ConnectAsync();

        var tools = await client.ListToolsAsync();

        Assert.Contains(tools, tool => tool.Name == "health_status");
        Assert.Contains(tools, tool => tool.Name == "detect_import_template");
        Assert.Contains(tools, tool => tool.Name == "get_ledger_period");
        Assert.Contains(tools, tool => tool.Name == "delete_reconciliation");
        Assert.True(tools.Count >= 25, $"Se esperaban al menos 25 tools, se obtuvieron {tools.Count}.");
    }

    [Fact]
    public async Task Tools_call_returns_content_and_structured_content()
    {
        await using var client = await ConnectAsync();

        var result = await client.CallToolAsync("health_status", new Dictionary<string, object?>());

        Assert.NotEqual(true, result.IsError);
        Assert.NotNull(result.StructuredContent);
    }

    [Fact]
    public async Task Requests_from_an_unapproved_origin_are_rejected()
    {
        var httpClient = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = JsonContent.Create(new { jsonrpc = "2.0", id = 1, method = "initialize", @params = new { protocolVersion = "2025-06-18" } })
        };
        request.Headers.Add("Origin", "https://unapproved.example");
        request.Headers.Add("Accept", "application/json, text/event-stream");

        using var response = await httpClient.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<McpClient> ConnectAsync()
    {
        var httpClient = factory.CreateClient();
        var transport = new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = new Uri(httpClient.BaseAddress!, "/mcp")
        }, httpClient);
        return await McpClient.CreateAsync(transport);
    }
}

public sealed class McpWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string inputDirectory = Path.Combine(Path.GetTempPath(), $"bancos-mcp-tests-{Guid.NewGuid():N}");

    public McpWebApplicationFactory()
    {
        Directory.CreateDirectory(inputDirectory);
        File.WriteAllText(Path.Combine(inputDirectory, "bcr.csv"), "oficina;fechaMovimiento;numeroDocumento;debito;credito;descripcion");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder) => builder
        .UseEnvironment("Testing")
        .ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["FileTemplateDetection:InputDirectory"] = inputDirectory,
            ["FileTemplateDetection:MaxFileSizeBytes"] = "1048576"
        }));

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && Directory.Exists(inputDirectory))
            Directory.Delete(inputDirectory, recursive: true);
    }
}
