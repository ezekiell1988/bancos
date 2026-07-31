using Bancos.Mcp.Tools;

namespace Bancos.Mcp.Features.Reconciliation;

public static class ReconciliationModule
{
    public static IServiceCollection AddReconciliationModule(this IServiceCollection services)
    {
        services.AddScoped<ReconciliationService>();
        services.AddSingleton<IMcpTool, ListUnreconciledTransactionsTool>();
        services.AddSingleton<IMcpTool, ProposeReconciliationTool>();
        services.AddSingleton<IMcpTool, ConfirmReconciliationTool>();
        services.AddSingleton<IMcpTool, CorrectReconciliationTool>();
        services.AddSingleton<IMcpTool, DeleteReconciliationTool>();
        return services;
    }
}