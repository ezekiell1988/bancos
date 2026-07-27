using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Bancos.Mcp.Features.Classification;

public sealed record AiClassificationSuggestion(string CategoryCode, decimal Confidence, string Reasoning);

public sealed class AzureAiClassifier(HttpClient httpClient, IOptions<ClassificationAiOptions> options)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<AiClassificationSuggestion?> ClassifyAsync(
        string normalizedDescription,
        IReadOnlyList<(string Code, string Name)> allowedCategories,
        CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (!settings.Enabled || string.IsNullOrWhiteSpace(settings.Endpoint) || string.IsNullOrWhiteSpace(settings.ApiKey))
            return null;

        var catalog = string.Join('\n', allowedCategories.Select(category => $"- {category.Code}: {category.Name}"));
        var prompt = $"""
            Clasifica la siguiente descripción normalizada de un movimiento bancario en una de las categorías del catálogo.
            Responde únicamente JSON con las claves categoryCode, confidence (0 a 1) y reasoning (máximo 20 palabras).
            Si ninguna categoría aplica con certeza razonable, usa categoryCode null y confidence 0.

            Descripción: {normalizedDescription}

            Catálogo:
            {catalog}
            """;

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{settings.Endpoint.TrimEnd('/')}/chat/completions");
            request.Headers.Add("api-key", settings.ApiKey);
            request.Content = JsonContent.Create(new
            {
                model = settings.Model,
                messages = new object[]
                {
                    new { role = "system", content = "Eres un clasificador contable determinista. Responde solo JSON válido." },
                    new { role = "user", content = prompt }
                },
                response_format = new { type = "json_object" },
                temperature = 0
            });

            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;

            var payload = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(JsonOptions, cancellationToken);
            var content = payload?.Choices?.FirstOrDefault()?.Message?.Content;
            if (string.IsNullOrWhiteSpace(content))
                return null;

            var suggestion = JsonSerializer.Deserialize<AiSuggestionPayload>(content, JsonOptions);
            if (suggestion is null || string.IsNullOrWhiteSpace(suggestion.CategoryCode))
                return null;

            return new AiClassificationSuggestion(suggestion.CategoryCode, (decimal)suggestion.Confidence, suggestion.Reasoning ?? string.Empty);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // El timeout del proveedor es un fallo de clasificación, no una cancelación del lote.
            return null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return null;
        }
    }

    private sealed record ChatCompletionResponse(List<Choice>? Choices);
    private sealed record Choice(ChatMessage? Message);
    private sealed record ChatMessage(string? Content);
    private sealed record AiSuggestionPayload(string? CategoryCode, double Confidence, string? Reasoning);
}
