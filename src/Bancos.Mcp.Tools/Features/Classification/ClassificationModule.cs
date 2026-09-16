using Bancos.Mcp.Tools;
using Microsoft.Extensions.Options;

namespace Bancos.Mcp.Features.Classification;

public static class ClassificationModule
{
    public static IServiceCollection AddClassificationModule(this IServiceCollection services)
    {
        services.AddOptions<ClassificationAiOptions>()
            .BindConfiguration(ClassificationAiOptions.Section)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddHttpClient<AzureAiClassifier>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<ClassificationAiOptions>>().Value;
            client.Timeout = TimeSpan.FromSeconds(options.RequestTimeoutSeconds);
        });
        services.AddScoped<ClassificationService>();
        return services;
    }
}
