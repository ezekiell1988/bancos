using System.Text.Json.Serialization;
using Bancos.Api.Data;
using Bancos.Api.Features.Accounts;
using Bancos.Api.Features.Classification;
using Bancos.Api.Features.Imports;
using Bancos.Api.Features.Loans;
using Bancos.Api.Features.Reports;
using Bancos.Api.Features.Transactions;
using Bancos.Api.Infrastructure;
using Hangfire;
using Hangfire.Console;
using Hangfire.SqlServer;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Configuration.AddLocalSecrets(builder.Environment.ContentRootPath);
builder.Services.AddProblemDetails(); builder.Services.AddHealthChecks();
builder.Services.AddSignalR();
builder.Services.AddOptions<StorageOptions>().BindConfiguration(StorageOptions.Section).ValidateDataAnnotations().ValidateOnStart();
if (!builder.Environment.IsEnvironment("Testing"))
{
    var connection = builder.Configuration.RequireConnectionString();
    builder.Services.AddDbContext<BancosDbContext>(options => options.UseSqlServer(connection));
    builder.Services.AddHangfire(config => config.UseSqlServerStorage(connection, new SqlServerStorageOptions()).UseConsole());
    builder.Services.AddHangfireServer();
}
builder.Services.AddAccountsModule().AddClassificationModule().AddImportsModule().AddReportsModule();
var app = builder.Build(); app.UseExceptionHandler(); app.MapHealthChecks("/_api/health");
if (!app.Environment.IsEnvironment("Testing")) app.UseHangfireDashboard("/_api/hangfire");
app.UseDefaultFiles(); app.UseStaticFiles();
app.MapAccountsEndpoints().MapClassificationEndpoints().MapImportsEndpoints().MapLoansEndpoints().MapReportsEndpoints().MapTransactionsEndpoints();
app.MapHub<ImportProgressHub>("/hubs/import-progress");
app.MapFallbackToFile("index.html");
app.Run();
public partial class Program;
