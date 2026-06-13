using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GLOWAPI.Infrastructure.Database;

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    private static readonly ServerVersion MySqlServerVersion = ServerVersion.Parse("8.0.36-mysql");

    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable(DatabaseConnectionResolver.HostedConnectionVariable)
            ?? "Server=localhost;Port=3306;Database=glowapi_db;User=root;Password=;";

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseMySql(connectionString, MySqlServerVersion);

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
