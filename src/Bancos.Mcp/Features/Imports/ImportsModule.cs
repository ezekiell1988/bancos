using Bancos.Mcp.Tools;

namespace Bancos.Mcp.Features.Imports;

public static class ImportsModule
{
    public static IServiceCollection AddImportsModule(this IServiceCollection services)
    {
        services.AddSingleton<ImportJobQueryService>();
        services.AddSingleton<IMcpTool, GetImportJobStatusTool>();
        services.AddSingleton<IMcpTool, ListRecentImportJobsTool>();
        services.AddSingleton<IMcpTool, RetryImportJobTool>();
        return services;
    }
}
