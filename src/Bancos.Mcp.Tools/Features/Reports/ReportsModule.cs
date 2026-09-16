namespace Bancos.Mcp.Features.Reports;

public static class ReportsModule
{
    public static IServiceCollection AddReportsModule(this IServiceCollection services)
    {
        services.AddScoped<ReportingService>();
        return services;
    }
}
