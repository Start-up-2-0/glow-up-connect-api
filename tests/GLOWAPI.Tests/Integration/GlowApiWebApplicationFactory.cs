using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GLOWAPI.Tests.Integration;

public class GlowApiWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"GlowApiTests_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Auth:MaxLoginAttempts"] = "5",
                ["Auth:LockoutMinutes"] = "15",
                ["Auth:SessionMinutes"] = "15",
                ["Auth:RefreshTokenDays"] = "7",
                ["Auth:SlidingRenewalMinutes"] = "30",
                ["Auth:TokenSalt"] = "glow-dev-token-salt-min-32-chars!!",
                ["Auth:TokenHeaderName"] = "x-glow-token",
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=test;Username=test;Password=test",
                ["Mensageria:Habilitado"] = "false"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<ApplicationDbContext>));
            services.RemoveAll(typeof(ApplicationDbContext));

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));

            ConfigureTestServices(services);
        });
    }

    protected virtual void ConfigureTestServices(IServiceCollection services)
    {
    }

    public async Task SeedUsuarioAsync(string email, string senha, int tentativas = 0, bool ativo = true)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<GLOWAPI.Application.Interfaces.Services.IPasswordHasher>();

        db.Usuarios.Add(new Usuario
        {
            Nome = "Usuario Teste",
            Email = email,
            Telefone = "11999999999",
            Senha = hasher.Hash(senha),
            Role = UserRole.Cliente,
            Tentativas = tentativas,
            Ativo = ativo
        });

        await db.SaveChangesAsync();
    }
}
