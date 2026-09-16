using System.ComponentModel;
using Bancos.Mcp.Domain;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace Bancos.Mcp.Features.Reports;

[McpServerToolType]
public static class GetBalanceSheetReportTool
{
    [McpServerTool(Name = "get_balance_sheet_report", Title = "Situación financiera en TOON",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Devuelve en TOON los listados de activos, pasivos y capital de un único período, "
               + "comparado contra el período anterior si existe, validando el equilibrio e incluyendo advertencia si faltan cierres.")]
    public static async Task<BalanceSheetReportResult> GetBalanceSheetReportAsync(
        ReportingService service,
        [Description("ID del período cuyo cierre se reporta (fecha de corte = fin del período).")] Guid periodId,
        CancellationToken cancellationToken = default)
    {
        BalanceSheetReport report;
        try
        {
            report = await service.GetBalanceSheetAsync(periodId, cancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            throw new McpException(exception.Message);
        }

        var toon = ReportToonFormatter.FormatBalanceSheet(report);
        var html = ReportHtmlRenderer.RenderBalanceSheet(report, CostaRicaTime.Now);

        var previousPeriod = report.PreviousPeriod is null
            ? null
            : new BalanceSheetPreviousPeriod(
                report.PreviousPeriod.PeriodId,
                report.PreviousPeriod.PeriodLabel,
                report.PreviousPeriod.TotalAssets,
                report.PreviousPeriod.TotalLiabilities,
                report.PreviousPeriod.Equity,
                report.PreviousPeriod.BalanceDifference,
                report.PreviousPeriod.AccountsMissingClosingCount);

        return new BalanceSheetReportResult(
            toon,
            html,
            report.TotalAssets,
            report.TotalLiabilities,
            report.Equity,
            report.BalanceDifference,
            report.AccountsMissingClosingCount,
            previousPeriod);
    }
}

public sealed record BalanceSheetPreviousPeriod(
    Guid PeriodId,
    string PeriodLabel,
    decimal TotalAssets,
    decimal TotalLiabilities,
    decimal Equity,
    decimal BalanceDifference,
    int AccountsMissingClosingCount);

public sealed record BalanceSheetReportResult(
    string Toon,
    string Html,
    decimal TotalAssets,
    decimal TotalLiabilities,
    decimal Equity,
    decimal BalanceDifference,
    int AccountsMissingClosingCount,
    BalanceSheetPreviousPeriod? PreviousPeriod);
