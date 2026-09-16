namespace Bancos.Mcp.Features.Reconciliation;

public static class ReconciliationModule
{
    public static IServiceCollection AddReconciliationModule(this IServiceCollection services)
    {
        services.AddScoped<ReconciliationService>();
        return services;
    }
}