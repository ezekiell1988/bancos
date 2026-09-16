using CompanyMcp.Tools;
using ModelContextProtocol.AspNetCore;
using ModelContextProtocol.Server;

namespace McpHttpServer;

public static class McpModule
{
    public static void AddMcpHttpServer(IServiceCollection services)
    {
        CompanyMcpCatalog.AddDomainServices(services);

        services.AddMcpServer()
            .WithHttpTransport(options =>
                options.SessionMode = HttpServerSessionMode.Stateless)
            .WithToolsFromAssembly(CompanyMcpCatalog.Assembly);
    }

    public static void MapMcpHttpServer(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapMcp("/mcp");
    }
}
