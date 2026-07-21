using Bancos.Mcp.Protocol;
using Bancos.Mcp.Tools;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();
builder.Services.AddOptions<McpOptions>()
    .BindConfiguration(McpOptions.Section)
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddSingleton<IMcpTool, StatusTool>();
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
