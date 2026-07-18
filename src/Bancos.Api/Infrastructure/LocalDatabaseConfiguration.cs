using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Bancos.Api.Infrastructure;

public static class LocalDatabaseConfiguration
{
    private const string LocalSecretsRelativePath = ".local-secrets/db.json";

    public static void AddLocalSecrets(this IConfigurationManager configuration, string contentRootPath)
    {
        var root = FindRepositoryRoot(contentRootPath);
        configuration
            .AddJsonFile(Path.Combine(root, "src/Bancos.Api/appsettings.Development.json"), optional: true, reloadOnChange: false)
            .AddJsonFile(Path.Combine(root, LocalSecretsRelativePath), optional: true, reloadOnChange: false);
    }

    public static IConfiguration BuildForDesignTime(string contentRootPath) => new ConfigurationBuilder()
        .SetBasePath(FindRepositoryRoot(contentRootPath))
        .AddJsonFile("src/Bancos.Api/appsettings.json", optional: false, reloadOnChange: false)
        .AddJsonFile("src/Bancos.Api/appsettings.Development.json", optional: true, reloadOnChange: false)
        .AddJsonFile(LocalSecretsRelativePath, optional: true, reloadOnChange: false)
        .AddEnvironmentVariables()
        .Build();

    public static string RequireConnectionString(this IConfiguration configuration)
    {
        var direct = new[]
            {
                configuration.GetConnectionString("DefaultConnection"),
                configuration["ConnectionString"],
                configuration["connectionString"]
            }
            .Concat(configuration.AsEnumerable()
                .Where(pair => pair.Key.EndsWith("ConnectionString", StringComparison.OrdinalIgnoreCase))
                .Select(pair => pair.Value))
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
        if (!string.IsNullOrWhiteSpace(direct))
        {
            var directBuilder = new SqlConnectionStringBuilder(direct) { TrustServerCertificate = true, ConnectTimeout = 10 };
            return directBuilder.ConnectionString;
        }

        var server = FindValue(configuration, "Server", "Host", "DataSource");
        var database = FindValue(configuration, "Database", "InitialCatalog");
        if (string.IsNullOrWhiteSpace(server) || string.IsNullOrWhiteSpace(database))
            throw new InvalidOperationException("A local SQL connection must be configured outside version control.");

        var port = FindValue(configuration, "Port");
        var dataSource = string.IsNullOrWhiteSpace(port) || server.Contains(',', StringComparison.Ordinal)
            ? server
            : $"{server},{port}";
        var builder = new SqlConnectionStringBuilder { DataSource = dataSource, InitialCatalog = database, TrustServerCertificate = true, ConnectTimeout = 10 };
        var user = FindValue(configuration, "User", "UserId", "Username");
        var password = FindValue(configuration, "Password");
        if (string.IsNullOrWhiteSpace(user)) builder.IntegratedSecurity = true;
        else { builder.UserID = user; builder.Password = password; }
        return builder.ConnectionString;
    }

    private static string FindRepositoryRoot(string startPath)
    {
        for (var directory = new DirectoryInfo(Path.GetFullPath(startPath)); directory is not null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "Bancos.sln"))) return directory.FullName;
        throw new InvalidOperationException("Repository root could not be located.");
    }

    private static string? FindValue(IConfiguration configuration, params string[] names) =>
        configuration.AsEnumerable().FirstOrDefault(pair => names.Any(name => pair.Key.EndsWith(name, StringComparison.OrdinalIgnoreCase))
            && !string.IsNullOrWhiteSpace(pair.Value)).Value;
}
