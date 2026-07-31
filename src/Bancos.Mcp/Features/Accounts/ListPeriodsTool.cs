using System.Globalization;
using System.Text.Json;
using Bancos.Mcp.Protocol;
using Bancos.Mcp.Tools;

namespace Bancos.Mcp.Features.Accounts;

public sealed class ListPeriodsTool(IServiceScopeFactory scopeFactory) : IMcpTool
{
    public McpToolDefinition Definition { get; } = new(
        Name: "list_periods",
        Title: "Listar períodos de reporte",
        Description: "Devuelve los períodos de reporte (ciclo de corte del 19 al 18) con su etiqueta y rango de fechas, paginado.",
        InputSchema: new
        {
            type = "object",
            properties = new
            {
                page = new
                {
                    type = new[] { "integer", "null" },
                    minimum = 1,
                    description = "Página a devolver, basada en 1 (por defecto 1)."
                },
                itemsPerPage = new
                {
                    type = new[] { "integer", "null" },
                    minimum = 1,
                    maximum = 200,
                    description = "Cantidad de períodos por página (por defecto 50; máximo 200)."
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
                page = new { type = "integer" },
                itemsPerPage = new { type = "integer" },
                totalItems = new { type = "integer" },
                totalPages = new { type = "integer" },
                periods = new
                {
                    type = "array",
                    items = new
                    {
                        type = "object",
                        properties = new
                        {
                            periodId = new { type = "string" },
                            label = new { type = "string" },
                            startDate = new { type = "string" },
                            endDate = new { type = "string" }
                        },
                        required = new[] { "periodId", "label", "startDate", "endDate" },
                        additionalProperties = false
                    }
                }
            },
            required = new[] { "page", "itemsPerPage", "totalItems", "totalPages", "periods" },
            additionalProperties = false
        });

    public async ValueTask<McpToolResult> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        var page = 1;
        if (arguments.TryGetProperty("page", out var pageEl) && pageEl.ValueKind == JsonValueKind.Number)
            page = Math.Max(pageEl.GetInt32(), 1);

        var itemsPerPage = 50;
        if (arguments.TryGetProperty("itemsPerPage", out var itemsPerPageEl) && itemsPerPageEl.ValueKind == JsonValueKind.Number)
            itemsPerPage = Math.Clamp(itemsPerPageEl.GetInt32(), 1, 200);

        using var scope = scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<AccountsQueryService>();
        var page_ = await service.ListPeriodsAsync(page, itemsPerPage, cancellationToken);

        var periods = page_.Items.Select(p => new
        {
            periodId = p.Id,
            label = p.Label,
            startDate = p.StartDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            endDate = p.EndDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
        }).ToList();

        var totalPages = Math.Max(1, (int)Math.Ceiling(page_.TotalItems / (double)page_.ItemsPerPage));
        var result = new { page_.Page, page_.ItemsPerPage, page_.TotalItems, totalPages, periods };
        var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        return new McpToolResult([McpContent.FromText(json)], result);
    }
}
