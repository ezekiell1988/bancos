using Bancos.Api.Data;
using Bancos.Api.Features.Accounts;
using Bancos.Api.Features.Imports;
using Bancos.Api.Features.Reports;
using Bancos.Api.Infrastructure;
using Hangfire;
using Hangfire.Console;
using Hangfire.SqlServer;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddLocalSecrets(builder.Environment.ContentRootPath);
builder.Services.AddProblemDetails(); builder.Services.AddHealthChecks();
builder.Services.AddOptions<StorageOptions>().BindConfiguration(StorageOptions.Section).ValidateDataAnnotations().ValidateOnStart();
if (!builder.Environment.IsEnvironment("Testing"))
{
    var connection = builder.Configuration.RequireConnectionString();
    builder.Services.AddDbContext<BancosDbContext>(options => options.UseSqlServer(connection));
    builder.Services.AddHangfire(config => config.UseSqlServerStorage(connection, new SqlServerStorageOptions()).UseConsole());
    builder.Services.AddHangfireServer();
}
builder.Services.AddAccountsModule().AddImportsModule().AddReportsModule();
var app = builder.Build(); app.UseExceptionHandler(); app.MapHealthChecks("/health");
if (!app.Environment.IsEnvironment("Testing")) app.UseHangfireDashboard("/hangfire");
app.MapAccountsEndpoints().MapImportsEndpoints().MapReportsEndpoints(); app.Run();
public partial class Program;
