using System.ComponentModel;
using Bancos.Mcp.Domain;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace Bancos.Mcp.Features.Reports;

[McpServerToolType]
public static class GetIncomeStatementReportTool
{
    [McpServerTool(Name = "get_income_statement_report", Title = "Estado de resultados en TOON",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Devuelve en TOON los listados de ingresos, gastos y resultado neto de un único período, "
               + "comparado contra el período anterior si existe, incluyendo advertencia si hay movimientos sin clasificar.")]
    public static async Task<IncomeStatementReportResult> GetIncomeStatementReportAsync(
        ReportingService service,
        [Description("ID del período a reportar.")] Guid periodId,
        CancellationToken cancellationToken = default)
    {
        IncomeStatementReport report;
        try
        {
            report = await service.GetIncomeStatementAsync(periodId, cancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            throw new McpException(exception.Message);
        }

        var toon = ReportToonFormatter.FormatIncomeStatement(report);
        var html = ReportHtmlRenderer.RenderIncomeStatement(report, CostaRicaTime.Now);

        var previousPeriod = report.PreviousPeriod is null
            ? null
            : new IncomeStatementPreviousPeriod(
                report.PreviousPeriod.PeriodId,
                report.PreviousPeriod.PeriodLabel,
                report.PreviousPeriod.TotalIncome,
                report.PreviousPeriod.TotalExpense,
                report.PreviousPeriod.NetResult,
                report.PreviousPeriod.PendingClassificationCount);

        return new IncomeStatementReportResult(
            toon,
            html,
            report.TotalIncome,
            report.TotalExpense,
            report.NetResult,
            report.PendingClassificationCount,
            previousPeriod);
    }
}

public sealed record IncomeStatementPreviousPeriod(
    Guid PeriodId,
    string PeriodLabel,
    decimal TotalIncome,
    decimal TotalExpense,
    decimal NetResult,
    int PendingClassificationCount);

public sealed record IncomeStatementReportResult(
    string Toon,
    string Html,
    decimal TotalIncome,
    decimal TotalExpense,
    decimal NetResult,
    int PendingClassificationCount,
    IncomeStatementPreviousPeriod? PreviousPeriod);
