using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
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
    public async Task Tools_list_exposes_the_safe_status_tool()
    {
        using var response = await PostAsync("""{"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}""");
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        var tool = document.RootElement.GetProperty("result").GetProperty("tools")[0];
        Assert.Equal("health_status", tool.GetProperty("name").GetString());
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
    protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.UseEnvironment("Testing");
}
