using Microsoft.Extensions.Configuration;

namespace GLOWAPI.Infrastructure.Database;

public static class DatabaseConnectionResolver
{
    public const string ProviderMySql = "MySQL";
    public const string HostedConnectionVariable = "MYSQL_CS";

    public static string ResolveConnectionString(IConfiguration configuration, string environmentName)
    {
        var isHosted = string.Equals(environmentName, "Production", StringComparison.OrdinalIgnoreCase)
            || string.Equals(environmentName, "Staging", StringComparison.OrdinalIgnoreCase);

        return isHosted
            ? configuration[HostedConnectionVariable] ?? string.Empty
            : configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
    }

    public static string ResolveProvider(IConfiguration configuration) =>
        configuration.GetValue<string>("Database:Provider") ?? ProviderMySql;
}
