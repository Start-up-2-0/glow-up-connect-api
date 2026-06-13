using GLOWAPI.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GLOWAPI.API.Configuration;

public static class DatabaseMigrationExtensions
{
    public static async Task ApplyPendingMigrationsAsync(this WebApplication app)
    {
        var environment = app.Environment;
        var applyOnStartup = app.Configuration.GetValue(
            "Database:ApplyMigrationsOnStartup",
            environment.IsStaging() || environment.IsProduction());

        if (!applyOnStartup)
        {
            return;
        }

        await using var scope = app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        var pendingMigrations = (await dbContext.Database.GetPendingMigrationsAsync()).ToList();
        if (pendingMigrations.Count == 0)
        {
            logger.LogInformation("Nenhuma migration pendente para aplicar.");
            return;
        }

        logger.LogInformation(
            "Aplicando {Count} migration(s) pendente(s): {Migrations}",
            pendingMigrations.Count,
            string.Join(", ", pendingMigrations));

        await dbContext.Database.MigrateAsync();

        logger.LogInformation("Migrations aplicadas com sucesso.");
    }
}
