using Bancos.Mcp.Tools;

namespace Bancos.Mcp.Features.Reports;

public static class ReportsModule
{
    public static IServiceCollection AddReportsModule(this IServiceCollection services)
    {
        services.AddScoped<ReportingService>();
        services.AddSingleton<IMcpTool, GetIncomeStatementReportTool>();
        services.AddSingleton<IMcpTool, GetBalanceSheetReportTool>();
        return services;
    }
}
