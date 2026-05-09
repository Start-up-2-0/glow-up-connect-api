
using Microsoft.Extensions.DependencyInjection;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Services;


namespace GLOWAPI.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IUsuarioService, UsuarioService>();

        return services;
    }
}