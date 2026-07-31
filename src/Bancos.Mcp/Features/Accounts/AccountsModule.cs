using Bancos.Mcp.Tools;

namespace Bancos.Mcp.Features.Accounts;

public static class AccountsModule
{
    public static IServiceCollection AddAccountsModule(this IServiceCollection services)
    {
        services.AddScoped<AccountsQueryService>();
        services.AddSingleton<IMcpTool, ListBankAccountsTool>();
        services.AddSingleton<IMcpTool, ListPeriodsTool>();
        return services;
    }
}
