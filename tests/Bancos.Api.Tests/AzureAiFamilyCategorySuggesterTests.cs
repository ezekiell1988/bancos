using System.Net;
using System.Text;
using System.Text.Json;
using Bancos.Api.Features.Classification;
using Microsoft.Extensions.Options;
using Xunit;

namespace Bancos.Api.Tests;

public sealed class AzureAiFamilyCategorySuggesterTests
{
    [Fact]
    public async Task Sends_only_normalized_description_and_category_catalog()
    {
        var handler = new RecordingHandler("""
            {"choices":[{"message":{"content":"{\"categoryName\":\"Supermercado\",\"createNew\":true,\"confidence\":0.91}"}}]}
            """);
        var options = Options.Create(new ClassificationAiOptions
        {
            Enabled = true,
            Endpoint = "https://example.openai.azure.com/openai/v1",
            ApiKey = "test-key",
            Model = "gpt-5.5",
            MinimumConfidence = 0.8m
        });
        var service = new AzureAiFamilyCategorySuggester(new HttpClient(handler), options);

        var suggestion = await service.SuggestAsync("COMPRA DE ALIMENTOS", ["Hogar"]);

        Assert.NotNull(suggestion);
        Assert.Equal("Supermercado", suggestion.CategoryName);
        Assert.Equal("https://example.openai.azure.com/openai/v1/chat/completions", handler.RequestUri?.ToString());
        Assert.Equal("test-key", handler.ApiKey);
        using var payload = JsonDocument.Parse(Assert.IsType<string>(handler.Body));
        Assert.Equal("gpt-5.5", payload.RootElement.GetProperty("model").GetString());
        Assert.Equal(2000, payload.RootElement.GetProperty("max_completion_tokens").GetInt32());
        var userData = payload.RootElement.GetProperty("messages")[1].GetProperty("content").GetString()!;
        Assert.Contains("COMPRA DE ALIMENTOS", userData);
        Assert.Contains("Hogar", userData);
        Assert.DoesNotContain("amount", userData, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("account", userData, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("filename", userData, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Rejects_low_confidence_or_unconfigured_suggestions()
    {
        var handler = new RecordingHandler("""
            {"choices":[{"message":{"content":"{\"categoryName\":\"Otro\",\"createNew\":true,\"confidence\":0.4}"}}]}
            """);
        var options = Options.Create(new ClassificationAiOptions { Enabled = true, Endpoint = "https://example.test/openai/v1", ApiKey = "test-key", MinimumConfidence = 0.8m });

        var suggestion = await new AzureAiFamilyCategorySuggester(new HttpClient(handler), options).SuggestAsync("DESCRIPCION", []);

        Assert.Null(suggestion);
    }

    private sealed class RecordingHandler(string responseJson) : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }
        public string? ApiKey { get; private set; }
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            ApiKey = request.Headers.GetValues("api-key").Single();
            Body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(responseJson, Encoding.UTF8, "application/json") };
        }
    }
}
