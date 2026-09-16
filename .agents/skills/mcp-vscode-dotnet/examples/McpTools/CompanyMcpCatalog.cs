using System.Reflection;
using CompanyMcp.Tools.Features.System;
using CompanyMcp.Tools.Features.Tickets;
using Microsoft.Extensions.DependencyInjection;

namespace CompanyMcp.Tools;

public static class CompanyMcpCatalog
{
    public static Assembly Assembly { get; } = typeof(GetServerTimeTool).Assembly;

    public static void AddDomainServices(IServiceCollection services)
    {
        services.AddSingleton<ITicketService, DemoTicketService>();
    }
}
