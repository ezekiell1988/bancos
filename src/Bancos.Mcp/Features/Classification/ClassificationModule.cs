namespace Bancos.Mcp.Features.Classification;

public static class ClassificationModule
{
    public static IServiceCollection AddClassificationModule(this IServiceCollection services) =>
        services.AddScoped<ClassificationService>();
}
