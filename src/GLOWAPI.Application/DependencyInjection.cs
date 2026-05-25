
using Microsoft.Extensions.DependencyInjection;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Options;
using GLOWAPI.Application.Services;


namespace GLOWAPI.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IUsuarioService, UsuarioService>();
        services.AddScoped<IConfirmacaoEmailService, ConfirmacaoEmailService>();
        services.AddScoped<IAvatarBase64Decoder, AvatarBase64Decoder>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAuthSessionService, AuthSessionService>();
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<ICurrentUserContext, CurrentUserContext>();
        services.AddScoped<IMensagemNotificacaoService, MensagemNotificacaoService>();
        services.AddScoped<IMensagemNotificacaoProcessadorService, MensagemNotificacaoProcessadorService>();
        services.AddScoped<IProvedorMensagemResolver, ProvedorMensagemResolver>();

        return services;
    }
}