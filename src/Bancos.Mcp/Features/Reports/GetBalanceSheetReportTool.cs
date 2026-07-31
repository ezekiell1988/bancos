using System.Text.Json;
using Bancos.Mcp.Domain;
using Bancos.Mcp.Protocol;
using Bancos.Mcp.Tools;

namespace Bancos.Mcp.Features.Reports;

public sealed class GetBalanceSheetReportTool(IServiceScopeFactory scopeFactory) : IMcpTool
{
    public McpToolDefinition Definition { get; } = new(
        Name: "get_balance_sheet_report",
        Title: "Situación financiera en TOON",
        Description: "Devuelve en TOON los listados de activos, pasivos y capital de un único período, "
                   + "comparado contra el período anterior si existe, validando el equilibrio e incluyendo advertencia si faltan cierres.",
        InputSchema: new
        {
            type = "object",
            properties = new
            {
                periodId = new
                {
                    type = "string",
                    format = "uuid",
                    description = "ID del período cuyo cierre se reporta (fecha de corte = fin del período)."
                }
            },
            required = new[] { "periodId" },
            additionalProperties = false
        },
        OutputSchema: new
        {
            type = "object",
            properties = new
            {
                toon = new { type = "string" },
                html = new { type = "string" },
                totalAssets = new { type = "number" },
                totalLiabilities = new { type = "number" },
                equity = new { type = "number" },
                balanceDifference = new { type = "number" },
                accountsMissingClosingCount = new { type = "integer" },
                previousPeriod = new { type = new[] { "object", "null" } }
            },
            required = new[] { "toon", "html", "totalAssets", "totalLiabilities", "equity", "balanceDifference", "accountsMissingClosingCount", "previousPeriod" },
            additionalProperties = false
        });

    public async ValueTask<McpToolResult> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        if (!arguments.TryGetProperty("periodId", out var periodIdEl) || !Guid.TryParse(periodIdEl.GetString(), out var periodId))
            return McpToolResult.Error("Se requiere 'periodId' como UUID válido.");

        using var scope = scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ReportingService>();

        BalanceSheetReport report;
        try
        {
            report = await service.GetBalanceSheetAsync(periodId, cancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            return McpToolResult.Error(exception.Message);
        }

        var toon = ReportToonFormatter.FormatBalanceSheet(report);
        var html = ReportHtmlRenderer.RenderBalanceSheet(report, CostaRicaTime.Now);
        var structured = new
        {
            toon,
            html,
            totalAssets = report.TotalAssets,
            totalLiabilities = report.TotalLiabilities,
            equity = report.Equity,
            balanceDifference = report.BalanceDifference,
            accountsMissingClosingCount = report.AccountsMissingClosingCount,
            previousPeriod = report.PreviousPeriod is null ? null : new
            {
                periodId = report.PreviousPeriod.PeriodId,
                periodLabel = report.PreviousPeriod.PeriodLabel,
                totalAssets = report.PreviousPeriod.TotalAssets,
                totalLiabilities = report.PreviousPeriod.TotalLiabilities,
                equity = report.PreviousPeriod.Equity,
                balanceDifference = report.PreviousPeriod.BalanceDifference,
                accountsMissingClosingCount = report.PreviousPeriod.AccountsMissingClosingCount
            }
        };
        return new McpToolResult([McpContent.FromText(toon)], structured);
    }
}
