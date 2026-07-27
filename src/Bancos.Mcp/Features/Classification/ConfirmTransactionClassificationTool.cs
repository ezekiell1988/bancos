using System.Text.Json;
using Bancos.Mcp.Domain;
using Bancos.Mcp.Protocol;
using Bancos.Mcp.Tools;

namespace Bancos.Mcp.Features.Classification;

public sealed class ConfirmTransactionClassificationTool(IServiceScopeFactory scopeFactory) : IMcpTool
{
    public McpToolDefinition Definition { get; } = new(
        Name: "confirm_transaction_classification",
        Title: "Confirmar clasificación manual de un movimiento",
        Description: "Registra la categoría confirmada por el usuario para un movimiento y crea o actualiza una regla determinista "
                   + "reutilizable (misma cuenta, descripción exacta y tipo de operación) para que futuras coincidencias no requieran "
                   + "revisión manual ni IA.",
        InputSchema: new
        {
            type = "object",
            properties = new
            {
                transactionId = new { type = "string", format = "uuid", description = "Movimiento a clasificar." },
                categoryId = new { type = "string", format = "uuid", description = "Categoría confirmada por el usuario." },
                place = new
                {
                    type = new[] { "string", "null" },
                    maxLength = 120,
                    description = "Lugar o comercio confirmado; se guarda en el movimiento y en la regla reutilizable."
                }
            },
            required = new[] { "transactionId", "categoryId" },
            additionalProperties = false
        },
        OutputSchema: new
        {
            type = "object",
            properties = new
            {
                classificationId = new { type = "string" },
                categoryId = new { type = "string" },
                ruleId = new { type = "string" }
            },
            required = new[] { "classificationId", "categoryId", "ruleId" },
            additionalProperties = false
        });

    public async ValueTask<McpToolResult> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        if (!arguments.TryGetProperty("transactionId", out var transactionIdEl) || !Guid.TryParse(transactionIdEl.GetString(), out var transactionId))
            return McpToolResult.Error("Se requiere 'transactionId' como UUID válido.");
        if (!arguments.TryGetProperty("categoryId", out var categoryIdEl) || !Guid.TryParse(categoryIdEl.GetString(), out var categoryId))
            return McpToolResult.Error("Se requiere 'categoryId' como UUID válido.");
        string? place = null;
        if (arguments.TryGetProperty("place", out var placeEl) && placeEl.ValueKind == JsonValueKind.String)
            place = placeEl.GetString();

        using var scope = scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ClassificationService>();

        TransactionClassification classification;
        try
        {
            classification = await service.ConfirmManualClassificationAsync(transactionId, categoryId, place, cancellationToken);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return McpToolResult.Error(ex.Message);
        }

        var result = new
        {
            classificationId = classification.Id,
            categoryId = classification.CategoryId,
            ruleId = classification.ClassificationRuleId
        };
        var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        return new McpToolResult([McpContent.FromText(json)], result);
    }
}
