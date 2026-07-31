using Bancos.Mcp.Tools;
using Hangfire;

namespace Bancos.Mcp.Features.AccountPeriodClosings;

public static class AccountPeriodClosingsModule
{
    // ENE-2026 — primer período sembrado; el job calcula desde aquí hacia adelante.
    private static readonly Guid EarliestPeriodId = Guid.Parse("60000000-0000-0000-0000-000000000001");

    private const string MonthlyClosingCron = "0 2 19 * *";

    public static IServiceCollection AddAccountPeriodClosingsModule(this IServiceCollection services)
    {
        services.AddScoped<CalculateAccountPeriodClosingsJob>();
        services.AddSingleton<IMcpTool, CalculatePeriodClosingsTool>();
        return services;
    }

    public static IApplicationBuilder UseAccountPeriodClosingsJobs(this IApplicationBuilder app)
    {
        RecurringJob.AddOrUpdate<CalculateAccountPeriodClosingsJob>(
            "calculate-period-closings",
            job => job.ExecuteAsync(EarliestPeriodId, null!),
            MonthlyClosingCron);
        return app;
    }
}
