using Bancos.Mcp.Tools;

namespace Bancos.Mcp.Features.Classification;

public static class ClassificationModule
{
    public static IServiceCollection AddClassificationModule(this IServiceCollection services)
    {
        services.AddOptions<ClassificationAiOptions>()
            .BindConfiguration(ClassificationAiOptions.Section)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddHttpClient<AzureAiClassifier>();
        services.AddScoped<ClassificationService>();
        services.AddSingleton<IMcpTool, ClassifyPendingTransactionsTool>();
        services.AddSingleton<IMcpTool, ListUnclassifiedTransactionsTool>();
        services.AddSingleton<IMcpTool, ConfirmTransactionClassificationTool>();
        return services;
    }
}
