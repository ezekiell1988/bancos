namespace Bancos.Mcp.Features.Loans;

public static class LoansModule
{
    public static IServiceCollection AddLoansModule(this IServiceCollection services)
    {
        services.AddScoped<LoansQueryService>();
        return services;
    }
}