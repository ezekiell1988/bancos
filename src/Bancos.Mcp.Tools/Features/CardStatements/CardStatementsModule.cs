namespace Bancos.Mcp.Features.CardStatements;

public static class CardStatementsModule
{
    public static IServiceCollection AddCardStatementsModule(this IServiceCollection services)
    {
        services.AddScoped<CardStatementsQueryService>();
        return services;
    }
}