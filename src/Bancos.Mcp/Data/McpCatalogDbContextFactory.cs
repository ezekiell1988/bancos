using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Bancos.Mcp.Data;

public sealed class McpCatalogDbContextFactory : IDesignTimeDbContextFactory<McpCatalogDbContext>
{
    public McpCatalogDbContext CreateDbContext(string[] args)
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var configurationDirectory = File.Exists(Path.Combine(currentDirectory, "appsettings.json"))
            ? currentDirectory
            : Path.Combine(currentDirectory, "src", "Bancos.Mcp");
        var configuration = new ConfigurationBuilder()
            .SetBasePath(configurationDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: false)
            .Build();
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is required for EF migrations.");
        var options = new DbContextOptionsBuilder<McpCatalogDbContext>()
            .UseSqlServer(connectionString)
            .Options;
        return new McpCatalogDbContext(options);
    }
}