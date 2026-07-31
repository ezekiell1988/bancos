using System.Text.Json;
using Bancos.Mcp.Protocol;
using Bancos.Mcp.Tools;

namespace Bancos.Mcp.Features.Reconciliation;

public sealed class ListUnreconciledTransactionsTool(IServiceScopeFactory scopeFactory) : IMcpTool
{
    public McpToolDefinition Definition { get; } = new(
        Name: "list_unreconciled_transactions",
        Title: "Listar partidas no conciliadas",
        Description: "Lista movimientos que todavía no pertenecen a una conciliación confirmada.",
        InputSchema: new
        {
            type = "object",
            properties = new
            {
                dateFrom = new { type = new[] { "string", "null" }, format = "date" },
                dateTo = new { type = new[] { "string", "null" }, format = "date" },
                limit = new { type = "integer", minimum = 1, maximum = 200, defaultValue = 100 }
            },
            additionalProperties = false
        });

    public async ValueTask<McpToolResult> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        DateOnly? dateFrom = null;
        DateOnly? dateTo = null;
        if (arguments.TryGetProperty("dateFrom", out var dateFromElement) && dateFromElement.ValueKind == JsonValueKind.String && !DateOnly.TryParse(dateFromElement.GetString(), out var parsedFrom))
            return McpToolResult.Error("'dateFrom' debe ser una fecha válida.");
        else if (arguments.TryGetProperty("dateFrom", out dateFromElement) && dateFromElement.ValueKind == JsonValueKind.String)
            dateFrom = DateOnly.Parse(dateFromElement.GetString()!);
        if (arguments.TryGetProperty("dateTo", out var dateToElement) && dateToElement.ValueKind == JsonValueKind.String && !DateOnly.TryParse(dateToElement.GetString(), out var parsedTo))
            return McpToolResult.Error("'dateTo' debe ser una fecha válida.");
        else if (arguments.TryGetProperty("dateTo", out dateToElement) && dateToElement.ValueKind == JsonValueKind.String)
            dateTo = DateOnly.Parse(dateToElement.GetString()!);

        var limit = arguments.TryGetProperty("limit", out var limitElement) && limitElement.TryGetInt32(out var requestedLimit)
            ? requestedLimit
            : 100;

        using var scope = scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ReconciliationService>();
        var result = await service.ListUnreconciledAsync(dateFrom, dateTo, limit, cancellationToken);
        return ReconciliationToolSupport.JsonResult(new { items = result, count = result.Count });
    }
}