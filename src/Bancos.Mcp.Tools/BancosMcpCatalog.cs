using Bancos.Mcp.Data;
using Bancos.Mcp.Features.Accounts;
using Bancos.Mcp.Features.AccountPeriodClosings;
using Bancos.Mcp.Features.CardStatements;
using Bancos.Mcp.Features.Classification;
using Bancos.Mcp.Features.ExchangeRates;
using Bancos.Mcp.Features.FileProcessing;
using Bancos.Mcp.Features.ForeignExchange;
using Bancos.Mcp.Features.Health;
using Bancos.Mcp.Features.Imports;
using Bancos.Mcp.Features.Ledger;
using Bancos.Mcp.Features.Loans;
using Bancos.Mcp.Features.Reconciliation;
using Bancos.Mcp.Features.Reports;
using Bancos.Mcp.Features.TemplateDetection;
using Bancos.Mcp.Features.Transactions;
using Bancos.Mcp.Protocol;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bancos.Mcp.Tools;

public static class BancosMcpCatalog
{
    public static System.Reflection.Assembly Assembly { get; } = typeof(BancosMcpCatalog).Assembly;

    public static IServiceCollection AddDomainServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache();
        services.AddSingleton<ILlmAuditService, LlmAuditService>();
        services.AddHealthModule();

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrWhiteSpace(connectionString))
            services.AddDbContext<McpCatalogDbContext>(options => options.UseSqlServer(connectionString));

        services.AddTemplateDetectionModule(configuration);
        services.AddFileProcessingModule(configuration);
        services.AddImportsModule();
        services.AddAccountPeriodClosingsModule();
        services.AddClassificationModule();
        services.AddExchangeRatesModule(configuration);
        services.AddLedgerModule();
        services.AddForeignExchangeModule();
        services.AddReportsModule();
        services.AddAccountsModule();
        services.AddTransactionsModule();
        services.AddCardStatementsModule();
        services.AddLoansModule();
        services.AddReconciliationModule();

        return services;
    }
}
