using Bancos.Mcp.Tools;

namespace Bancos.Mcp.Features.CardStatements;

public static class CardStatementsModule
{
    public static IServiceCollection AddCardStatementsModule(this IServiceCollection services)
    {
        services.AddScoped<CardStatementsQueryService>();
        services.AddSingleton<IMcpTool, ListCardStatementsTool>();
        services.AddSingleton<IMcpTool, ListCardFinancingsTool>();
        return services;
    }
}