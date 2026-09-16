using System.ComponentModel;
using System.Globalization;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace Bancos.Mcp.Features.Accounts;

[McpServerToolType]
public static class ListPeriodsTool
{
    [McpServerTool(Name = "list_periods", Title = "Listar períodos de reporte",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Devuelve los períodos de reporte (ciclo de corte del 19 al 18) con su etiqueta y rango de fechas, paginado.")]
    public static async Task<ListPeriodsResult> ListPeriodsAsync(
        AccountsQueryService service,
        [Description("Página a devolver, basada en 1 (por defecto 1).")] int page = 1,
        [Description("Cantidad de períodos por página (por defecto 50; máximo 200).")] int itemsPerPage = 50,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        itemsPerPage = Math.Clamp(itemsPerPage, 1, 200);

        var page_ = await service.ListPeriodsAsync(page, itemsPerPage, cancellationToken);

        var periods = page_.Items.Select(p => new PeriodItem(
            p.Id,
            p.Label,
            p.StartDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            p.EndDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))).ToList();

        var totalPages = Math.Max(1, (int)Math.Ceiling(page_.TotalItems / (double)page_.ItemsPerPage));
        return new ListPeriodsResult(page_.Page, page_.ItemsPerPage, page_.TotalItems, totalPages, periods);
    }
}

public sealed record PeriodItem(Guid PeriodId, string Label, string StartDate, string EndDate);

public sealed record ListPeriodsResult(
    int Page,
    int ItemsPerPage,
    int TotalItems,
    int TotalPages,
    IReadOnlyList<PeriodItem> Periods);
