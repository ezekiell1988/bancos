namespace Bancos.Mcp.Features.Transactions;

public static class TransactionsModule
{
    public static IServiceCollection AddTransactionsModule(this IServiceCollection services)
    {
        services.AddScoped<TransactionsQueryService>();
        return services;
    }
}
