using Bancos.Mcp.Tools;

namespace Bancos.Mcp.Features.Ledger;

public static class LedgerModule
{
    public static IServiceCollection AddLedgerModule(this IServiceCollection services)
    {
        services.AddScoped<LedgerQueryService>();
        services.AddSingleton<IMcpTool, GetLedgerPeriodTool>();
        return services;
    }
}