namespace Bancos.Mcp.Features.Accounts;

public static class AccountsModule
{
    public static IServiceCollection AddAccountsModule(this IServiceCollection services)
    {
        services.AddScoped<AccountsQueryService>();
        return services;
    }
}
