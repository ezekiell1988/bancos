using Bancos.Mcp.Tools;

namespace Bancos.Mcp.Features.ForeignExchange;

public static class ForeignExchangeModule
{
    public static IServiceCollection AddForeignExchangeModule(this IServiceCollection services)
    {
        services.AddScoped<ForeignExchangeService>();
        services.AddSingleton<IMcpTool, CalculateForeignExchangeClosingTool>();
        return services;
    }
}