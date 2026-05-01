using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GLOWAPI.Infrastructure
{
   public static class DependencyInjection
   {
      public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        string environmentName)
      {

        var provider = configuration.GetValue<string>("Database:Provider") ?? "PostgreSQL";
        var isProduction = string.Equals(environmentName, "Production", StringComparison.OrdinalIgnoreCase);
        var connectionString = isProduction
            ? configuration["POSTGSL"]
            : configuration.GetConnectionString("DefaultConnection");

        if (!provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("O provedor de banco configurado para o projeto deve ser PostgreSQL.");
        }

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string do PostgreSQL não configurada. Use ConnectionStrings:DefaultConnection em desenvolvimento ou a variável de ambiente POSTGSL em produção.");
        }
        
        services.AddDbContext<ApplicationDbContext>(options => 
        {
            options.UseNpgsql(connectionString);
        });

        return services;
      
      }
   }   
}
