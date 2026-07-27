using System.Text.Json;
using Bancos.Mcp.Protocol;
using Bancos.Mcp.Tools;

namespace Bancos.Mcp.Features.Classification;

public sealed class ClassifyPendingTransactionsTool(IServiceScopeFactory scopeFactory) : IMcpTool
{
    public McpToolDefinition Definition { get; } = new(
        Name: "classify_pending_transactions",
        Title: "Clasificar movimientos pendientes por lote",
        Description: "Busca movimientos sin ningún intento de clasificación y aplica el motor determinista: "
                   + "primero reglas .NET por cuenta y descripción; si no hay coincidencia y la clasificación por IA está habilitada, "
                   + "consulta Azure AI solo con la descripción normalizada y el catálogo de categorías permitido. "
                   + "Si ninguna de las dos alcanza confianza suficiente, el movimiento queda 'No clasificado' en cola de revisión manual.",
        InputSchema: new
        {
            type = "object",
            properties = new
            {
                bankAccountId = new
                {
                    type = new[] { "string", "null" },
                    format = "uuid",
                    description = "Limita el lote a una cuenta bancaria específica; omite para procesar todas las cuentas."
                },
                limit = new
                {
                    type = new[] { "integer", "null" },
                    minimum = 1,
                    maximum = 500,
                    description = "Máximo de movimientos a procesar en esta llamada (por defecto 100)."
                }
            },
            required = Array.Empty<string>(),
            additionalProperties = false
        },
        OutputSchema: new
        {
            type = "object",
            properties = new
            {
                processed = new { type = "integer" },
                bySource = new
                {
                    type = "object",
                    properties = new
                    {
                        rule = new { type = "integer" },
                        ai = new { type = "integer" },
                        unclassified = new { type = "integer" }
                    },
                    required = new[] { "rule", "ai", "unclassified" },
                    additionalProperties = false
                }
            },
            required = new[] { "processed", "bySource" },
            additionalProperties = false
        });

    public async ValueTask<McpToolResult> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        Guid? bankAccountId = null;
        if (arguments.TryGetProperty("bankAccountId", out var accountEl) && accountEl.ValueKind == JsonValueKind.String)
        {
            if (!Guid.TryParse(accountEl.GetString(), out var parsedAccountId))
                return McpToolResult.Error("'bankAccountId' debe ser un UUID válido.");
            bankAccountId = parsedAccountId;
        }

        var limit = 100;
        if (arguments.TryGetProperty("limit", out var limitEl) && limitEl.ValueKind == JsonValueKind.Number)
            limit = Math.Clamp(limitEl.GetInt32(), 1, 500);

        using var scope = scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ClassificationService>();
        var summary = await service.ClassifyPendingAsync(bankAccountId, limit, cancellationToken);

        var result = new
        {
            processed = summary.Processed,
            bySource = new { rule = summary.Rule, ai = summary.Ai, unclassified = summary.Unclassified }
        };
        var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        return new McpToolResult([McpContent.FromText(json)], result);
    }
}
