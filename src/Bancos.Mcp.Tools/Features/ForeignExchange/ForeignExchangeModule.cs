namespace Bancos.Mcp.Features.ForeignExchange;

public static class ForeignExchangeModule
{
    public static IServiceCollection AddForeignExchangeModule(this IServiceCollection services)
    {
        services.AddScoped<ForeignExchangeService>();
        return services;
    }
}