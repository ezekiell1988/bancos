using Bancos.Mcp.Protocol;
using Bancos.Mcp.Tools;
using Bancos.Mcp.Data;
using Bancos.Mcp.Features.Health;
using Bancos.Mcp.Features.TemplateDetection;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddHealthModule();
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrWhiteSpace(connectionString))
    builder.Services.AddDbContext<McpCatalogDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddOptions<McpOptions>()
    .BindConfiguration(McpOptions.Section)
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddSingleton<IMcpTool, StatusTool>();
builder.Services.AddSingleton<ToolRegistry>();
builder.Services.AddTemplateDetectionModule(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();
app.UseRateLimiter();
if (!app.Environment.IsEnvironment("Testing"))
    app.UseHttpsRedirection();

app.MapHealthEndpoints();
app.MapMcpEndpoints();

app.Run();

public partial class Program;
