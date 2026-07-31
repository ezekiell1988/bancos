using Bancos.Mcp.Tools;

namespace Bancos.Mcp.Features.Transactions;

public static class TransactionsModule
{
    public static IServiceCollection AddTransactionsModule(this IServiceCollection services)
    {
        services.AddScoped<TransactionsQueryService>();
        services.AddSingleton<IMcpTool, SearchTransactionsTool>();
        services.AddSingleton<IMcpTool, GetTransactionDetailTool>();
        return services;
    }
}
