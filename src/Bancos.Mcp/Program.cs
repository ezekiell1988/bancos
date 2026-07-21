using Bancos.Mcp.Protocol;
using Bancos.Mcp.Tools;
using Bancos.Mcp.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrWhiteSpace(connectionString))
    builder.Services.AddDbContext<McpCatalogDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddOptions<McpOptions>()
    .BindConfiguration(McpOptions.Section)
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<FileTemplateDetectionOptions>()
    .BindConfiguration(FileTemplateDetectionOptions.Section)
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddSingleton<ImportTemplateDetectionService>();
builder.Services.AddSingleton<IMcpTool, StatusTool>();
builder.Services.AddSingleton<IMcpTool, DetectImportTemplateTool>();
builder.Services.AddSingleton<ToolRegistry>();

var app = builder.Build();

app.UseExceptionHandler();
if (!app.Environment.IsEnvironment("Testing"))
    app.UseHttpsRedirection();

app.MapHealthChecks("/_health");
app.MapGet("/{**path}", McpHandler.GetHealth);
app.MapPost("/{**path}", McpHandler.HandleAsync);

app.Run();

public partial class Program;
