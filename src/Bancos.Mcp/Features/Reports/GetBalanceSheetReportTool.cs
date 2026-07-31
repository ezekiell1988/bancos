using System.Text.Json;
using Bancos.Mcp.Domain;
using Bancos.Mcp.Protocol;
using Bancos.Mcp.Tools;

namespace Bancos.Mcp.Features.Reports;

public sealed class GetBalanceSheetReportTool(IServiceScopeFactory scopeFactory) : IMcpTool
{
    public McpToolDefinition Definition { get; } = new(
        Name: "get_balance_sheet_report",
        Title: "Reporte HTML de situación financiera",
        Description: "Genera un HTML autocontenido con activos, pasivos y capital al cierre de un período, "
                   + "validando que activos = pasivos + capital e incluyendo advertencia si faltan cierres por calcular.",
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
                html = new { type = "string" },
                totalAssets = new { type = "number" },
                totalLiabilities = new { type = "number" },
                equity = new { type = "number" },
                accountsMissingClosingCount = new { type = "integer" }
            },
            required = new[] { "html", "totalAssets", "totalLiabilities", "equity", "accountsMissingClosingCount" },
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

        var html = ReportHtmlRenderer.RenderBalanceSheet(report, CostaRicaTime.Now);
        var structured = new
        {
            html,
            totalAssets = report.TotalAssets,
            totalLiabilities = report.TotalLiabilities,
            equity = report.Equity,
            accountsMissingClosingCount = report.AccountsMissingClosingCount
        };
        return new McpToolResult([McpContent.FromText(html)], structured);
    }
}
