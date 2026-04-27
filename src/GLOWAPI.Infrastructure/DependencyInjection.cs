using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pomelo.EntityFrameworkCore.MySql;
using Npgsql.EntityFrameworkCore.PostgreSQL;

namespace GLOWAPI.Infrastructure
{
   public static class DependencyInjection
   {
      public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
      {

        var provider = configuration.GetValue<string>("DatabaseProvider");
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        
        services.AddDbContext<ApplicationDbContext>(options => 
        {
            if (provider == "SqlServer")
            {
                options.UseSqlServer(connectionString);
            } 
            else if (provider == "PostgreSQL")
            {
                options.UseNpgsql(connectionString);
            }
            else if (provider == "Mysql")
            {
                options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
            }
            else 
            {
                options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
            }
        });

        return services;
      
      }
   }   
}