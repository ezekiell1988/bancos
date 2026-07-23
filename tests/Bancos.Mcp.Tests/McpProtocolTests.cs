using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Bancos.Mcp.Features.TemplateDetection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bancos.Mcp.Tests;

public sealed class McpProtocolTests : IClassFixture<McpWebApplicationFactory>
{
    private readonly HttpClient client;
    private readonly McpWebApplicationFactory factory;

    public McpProtocolTests(McpWebApplicationFactory factory)
    {
        this.factory = factory;
        client = factory.CreateClient();
    }

    [Fact]
    public async Task Initialize_negotiates_the_client_protocol_version_and_creates_a_session()
    {
        using var response = await InitializeAsync("2024-11-05");
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("2024-11-05", document.RootElement.GetProperty("result").GetProperty("protocolVersion").GetString());
        Assert.True(response.Headers.TryGetValues("Mcp-Session-Id", out var values));
        Assert.False(string.IsNullOrWhiteSpace(values.Single()));
    }

    [Fact]
    public async Task Tools_list_requires_a_session_and_exposes_output_schemas()
    {
        var sessionId = await GetSessionIdAsync();
        using var response = await PostAsync("""{"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}""", sessionId);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tools = document.RootElement.GetProperty("result").GetProperty("tools");
        var statusTool = tools.EnumerateArray().Single(tool => tool.GetProperty("name").GetString() == "health_status");
        Assert.True(statusTool.TryGetProperty("outputSchema", out _));
        Assert.Contains(tools.EnumerateArray(), tool => tool.GetProperty("name").GetString() == "detect_import_template");
    }

    [Fact]
    public async Task Tools_call_returns_text_and_structured_content()
    {
        var sessionId = await GetSessionIdAsync();
        using var response = await PostAsync("""{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"health_status","arguments":{}}}""", sessionId);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        var result = document.RootElement.GetProperty("result");
        Assert.Equal("Estado: disponible", result.GetProperty("content")[0].GetProperty("text").GetString());
        Assert.Equal("available", result.GetProperty("structuredContent").GetProperty("status").GetString());
    }

    [Fact]
    public async Task Detect_import_template_returns_only_the_safe_structured_id()
    {
        var sessionId = await GetSessionIdAsync();
        using var response = await PostAsync("""{"jsonrpc":"2.0","id":5,"method":"tools/call","params":{"name":"detect_import_template","arguments":{"relativePath":"bcr.csv"}}}""", sessionId);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal("10000000-0000-0000-0000-000000000001", document.RootElement.GetProperty("result").GetProperty("structuredContent").GetProperty("idImportTemplates").GetString());
    }

    [Fact]
    public async Task Requests_with_an_unknown_session_or_protocol_version_are_rejected()
    {
        using var unknownSession = await PostAsync("""{"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}""", "unknown");
        var sessionId = await GetSessionIdAsync();
        using var unsupportedVersion = await PostAsync("""{"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}""", sessionId, "2020-01-01");

        Assert.Equal(HttpStatusCode.BadRequest, unknownSession.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, unsupportedVersion.StatusCode);
    }

    [Fact]
    public async Task Requests_from_an_unapproved_origin_are_rejected()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = JsonContent.Create(JsonDocument.Parse("""{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-06-18"}}""").RootElement)
        };
        request.Headers.Add("Origin", "https://unapproved.example");
        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ends_the_session()
    {
        var sessionId = await GetSessionIdAsync();
        using var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, "/mcp");
        deleteRequest.Headers.Add("Mcp-Session-Id", sessionId);
        using var deleteResponse = await client.SendAsync(deleteRequest);
        using var laterRequest = await PostAsync("""{"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}""", sessionId);

        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, laterRequest.StatusCode);
    }

    [Fact]
    public async Task Notifications_are_accepted_without_a_response_body()
    {
        var sessionId = await GetSessionIdAsync();
        using var notification = await PostAsync("""{"jsonrpc":"2.0","method":"notifications/initialized"}""", sessionId);

        Assert.Equal(HttpStatusCode.Accepted, notification.StatusCode);
    }

    [Fact]
    public void Mcp_post_endpoint_requires_the_bounded_concurrency_policy()
    {
        var dataSource = factory.Services.GetRequiredService<EndpointDataSource>();
        var endpoint = dataSource.Endpoints.Single(endpoint =>
            endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods.Contains("POST") == true
            && endpoint is RouteEndpoint routeEndpoint
            && routeEndpoint.RoutePattern.RawText == "/mcp/");

        Assert.Equal(TemplateDetectionModule.McpToolsRateLimitPolicy, endpoint.Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName);
    }

    private async Task<string> GetSessionIdAsync()
    {
        using var response = await InitializeAsync("2025-06-18");
        response.EnsureSuccessStatusCode();
        return response.Headers.GetValues("Mcp-Session-Id").Single();
    }

    private Task<HttpResponseMessage> InitializeAsync(string version) =>
        PostAsync(JsonSerializer.Serialize(new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "initialize",
            @params = new { protocolVersion = version }
        }));

    private Task<HttpResponseMessage> PostAsync(string json, string? sessionId = null, string protocolVersion = "2025-06-18")
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/mcp") { Content = JsonContent.Create(JsonDocument.Parse(json).RootElement) };
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            request.Headers.Add("Mcp-Session-Id", sessionId);
            request.Headers.Add("MCP-Protocol-Version", protocolVersion);
        }

        return client.SendAsync(request);
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
