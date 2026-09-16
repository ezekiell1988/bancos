using McpHttpServer;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();
McpModule.AddMcpHttpServer(builder.Services);

var app = builder.Build();

app.MapHealthChecks("/health").AllowAnonymous();
McpModule.MapMcpHttpServer(app);

app.Run();

public partial class Program;
