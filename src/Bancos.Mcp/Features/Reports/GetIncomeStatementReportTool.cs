using System.Text.Json;
using Bancos.Mcp.Domain;
using Bancos.Mcp.Protocol;
using Bancos.Mcp.Tools;

namespace Bancos.Mcp.Features.Reports;

public sealed class GetIncomeStatementReportTool(IServiceScopeFactory scopeFactory) : IMcpTool
{
    public McpToolDefinition Definition { get; } = new(
        Name: "get_income_statement_report",
        Title: "Estado de resultados en TOON",
        Description: "Devuelve en TOON los listados de ingresos, gastos y resultado neto de un único período, "
                   + "comparado contra el período anterior si existe, incluyendo advertencia si hay movimientos sin clasificar.",
        InputSchema: new
        {
            type = "object",
            properties = new
            {
                periodId = new
                {
                    type = "string",
                    format = "uuid",
                    description = "ID del período a reportar."
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
                totalIncome = new { type = "number" },
                totalExpense = new { type = "number" },
                netResult = new { type = "number" },
                pendingClassificationCount = new { type = "integer" },
                previousPeriod = new { type = new[] { "object", "null" } }
            },
            required = new[] { "toon", "html", "totalIncome", "totalExpense", "netResult", "pendingClassificationCount", "previousPeriod" },
            additionalProperties = false
        });

    public async ValueTask<McpToolResult> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        if (!arguments.TryGetProperty("periodId", out var periodIdEl) || !Guid.TryParse(periodIdEl.GetString(), out var periodId))
            return McpToolResult.Error("Se requiere 'periodId' como UUID válido.");

        using var scope = scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ReportingService>();

        IncomeStatementReport report;
        try
        {
            report = await service.GetIncomeStatementAsync(periodId, cancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            return McpToolResult.Error(exception.Message);
        }

        var toon = ReportToonFormatter.FormatIncomeStatement(report);
        var html = ReportHtmlRenderer.RenderIncomeStatement(report, CostaRicaTime.Now);
        var structured = new
        {
            toon,
            html,
            totalIncome = report.TotalIncome,
            totalExpense = report.TotalExpense,
            netResult = report.NetResult,
            pendingClassificationCount = report.PendingClassificationCount,
            previousPeriod = report.PreviousPeriod is null ? null : new
            {
                periodId = report.PreviousPeriod.PeriodId,
                periodLabel = report.PreviousPeriod.PeriodLabel,
                totalIncome = report.PreviousPeriod.TotalIncome,
                totalExpense = report.PreviousPeriod.TotalExpense,
                netResult = report.PreviousPeriod.NetResult,
                pendingClassificationCount = report.PreviousPeriod.PendingClassificationCount
            }
        };
        return new McpToolResult([McpContent.FromText(toon)], structured);
    }
}
