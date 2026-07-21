using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Bancos.Mcp.Tools;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Bancos.Mcp.Tests;

public sealed class McpProtocolTests : IClassFixture<McpWebApplicationFactory>
{
    private readonly HttpClient client;

    public McpProtocolTests(McpWebApplicationFactory factory) => client = factory.CreateClient();

    [Fact]
    public async Task Initialize_negotiates_the_client_protocol_version()
    {
        using var response = await PostAsync("""{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05"}}""");
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("2024-11-05", document.RootElement.GetProperty("result").GetProperty("protocolVersion").GetString());
    }

    [Fact]
    public async Task Copilot_batch_variant_is_normalized_to_initialize()
    {
        using var response = await PostAsync("""[{"jsonrpc":"2.0"}]""");
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("bancos-mcp", document.RootElement.GetProperty("result").GetProperty("serverInfo").GetProperty("name").GetString());
    }

    [Fact]
    public async Task Tools_list_exposes_the_safe_tools()
    {
        using var response = await PostAsync("""{"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}""");
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        var tools = document.RootElement.GetProperty("result").GetProperty("tools");
        Assert.Contains(tools.EnumerateArray(), tool => tool.GetProperty("name").GetString() == "health_status");
        Assert.Contains(tools.EnumerateArray(), tool => tool.GetProperty("name").GetString() == "detect_import_template");
    }

    [Fact]
    public async Task Tools_call_returns_the_required_text_content_shape()
    {
        using var response = await PostAsync("""{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"health_status","arguments":{}}}""");
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        var content = document.RootElement.GetProperty("result").GetProperty("content")[0];
        Assert.Equal("text", content.GetProperty("type").GetString());
        Assert.Equal("Estado: disponible", content.GetProperty("text").GetString());
    }

    [Fact]
    public async Task Tools_call_returns_only_the_import_template_id()
    {
        using var response = await PostAsync("""{"jsonrpc":"2.0","id":5,"method":"tools/call","params":{"name":"detect_import_template","arguments":{"relativePath":"bcr.csv"}}}""");
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        var content = document.RootElement.GetProperty("result").GetProperty("content")[0];
        Assert.Equal("text", content.GetProperty("type").GetString());
        Assert.Equal("idImportTemplates: 10000000-0000-0000-0000-000000000001", content.GetProperty("text").GetString());
    }

    [Fact]
    public async Task Tools_call_returns_a_safe_error_for_a_path_outside_the_input_directory()
    {
        using var response = await PostAsync("""{"jsonrpc":"2.0","id":6,"method":"tools/call","params":{"name":"detect_import_template","arguments":{"relativePath":"../outside.csv"}}}""");
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        var error = document.RootElement.GetProperty("error");
        Assert.Equal(-32602, error.GetProperty("code").GetInt32());
        Assert.DoesNotContain("outside.csv", error.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Notifications_and_client_responses_are_accepted_without_a_body()
    {
        using var notification = await PostAsync("""{"jsonrpc":"2.0","method":"notifications/initialized"}""");
        using var clientResponse = await PostAsync("""{"jsonrpc":"2.0","id":4,"result":{}}""");

        Assert.Equal(HttpStatusCode.Accepted, notification.StatusCode);
        Assert.Equal(HttpStatusCode.Accepted, clientResponse.StatusCode);
    }

    private Task<HttpResponseMessage> PostAsync(string json) => client.PostAsync("/", JsonContent.Create(JsonDocument.Parse(json).RootElement));
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
