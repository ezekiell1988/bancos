using Bancos.Mcp.Features.Parsing;
using Bancos.Mcp.Tools;
using Hangfire;
using Hangfire.Console;

namespace Bancos.Mcp.Features.FileProcessing;

public static class FileProcessingModule
{
    public static IServiceCollection AddFileProcessingModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<BcrDebitCsvParser>();
        services.AddTransient<AccountMovementSpreadsheetParser>();
        services.AddTransient<BacCreditFinancingXlsParser>();
        services.AddTransient<CardStatementParser>();
        services.AddTransient<CoopealianzaLoanPdfParser>();
        services.AddTransient<BacAccountStatementPdfParser>();
        services.AddTransient<BnCardStatementPdfParser>();

        services.AddScoped<AccountResolver>();
        services.AddScoped<ImportFileJob>();
        services.AddSingleton<IMcpTool, ProcessImportFileTool>();

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddHangfire(config => config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseSqlServerStorage(connectionString)
                .UseConsole());
            services.AddHangfireServer();
        }

        return services;
    }
}
