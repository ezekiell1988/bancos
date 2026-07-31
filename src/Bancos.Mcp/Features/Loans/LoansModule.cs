using Bancos.Mcp.Tools;

namespace Bancos.Mcp.Features.Loans;

public static class LoansModule
{
    public static IServiceCollection AddLoansModule(this IServiceCollection services)
    {
        services.AddScoped<LoansQueryService>();
        services.AddSingleton<IMcpTool, ListLoanStatementsTool>();
        return services;
    }
}