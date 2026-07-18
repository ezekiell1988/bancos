using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Bancos.Api.Infrastructure;

namespace Bancos.Api.Data;
public sealed class BancosDbContextFactory : IDesignTimeDbContextFactory<BancosDbContext>
{
    public BancosDbContext CreateDbContext(string[] args)
    {
        var configuration = LocalDatabaseConfiguration.BuildForDesignTime(Directory.GetCurrentDirectory());
        return new(new DbContextOptionsBuilder<BancosDbContext>().UseSqlServer(configuration.RequireConnectionString()).Options);
    }
}
