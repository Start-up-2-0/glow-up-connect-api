
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
        services.AddScoped<IPlanoService, PlanoService>();
        services.AddScoped<IAssinaturaService, AssinaturaService>();
        services.AddScoped<IAssinaturaHistoricoService, AssinaturaHistoricoService>();
        services.AddScoped<IEstabelecimentoPerfilService, EstabelecimentoPerfilService>();
        services.AddScoped<IProfissionalAutonomoPerfilService, ProfissionalAutonomoPerfilService>();
        services.AddScoped<IModulosAssinaturaService, ModulosAssinaturaService>();
        services.AddScoped<IMatrizPermissaoNegocioService, MatrizPermissaoNegocioService>();
        services.AddScoped<IAutorizacaoNegocioService, AutorizacaoNegocioService>();
        services.AddScoped<IProfissionalEscopoAcessoService, ProfissionalEscopoAcessoService>();
        services.AddScoped<IEquipeNegocioService, EquipeNegocioService>();
        services.AddScoped<IAgendaNegocioService, AgendaNegocioService>();
        services.AddScoped<IAtendimentoProfissionalService, AtendimentoProfissionalService>();
        services.AddScoped<ICaixaNegocioService, CaixaNegocioService>();
        services.AddScoped<IProfissionalServicoNegocioService, ProfissionalServicoNegocioService>();
        services.AddScoped<IServicoNegocioService, ServicoNegocioService>();
        services.AddScoped<IServicoProfissionalAutonomoService, ServicoProfissionalAutonomoService>();
        services.AddScoped<IHorarioFuncionamentoNegocioService, HorarioFuncionamentoNegocioService>();
        services.AddScoped<IHorarioProfissionalNegocioService, HorarioProfissionalNegocioService>();
        services.AddScoped<IHorarioProfissionalAutonomoService, HorarioProfissionalAutonomoService>();
        services.AddScoped<IDisponibilidadeAgendaService, DisponibilidadeAgendaService>();
        services.AddScoped<IAuditoriaNegocioService, AuditoriaNegocioService>();
        services.AddScoped<IEquipeNotificacaoService, EquipeNotificacaoService>();
        services.AddScoped<IUsuarioNegocioContextoService, UsuarioNegocioContextoService>();
        services.AddScoped<IConviteNegocioService, ConviteNegocioService>();
        services.AddScoped<IAssinaturaNotificacaoService, AssinaturaNotificacaoService>();
        services.AddScoped<IGatewayPagamentoResolver, GatewayPagamentoResolver>();
        services.AddScoped<IWebhookPagamentoService, WebhookPagamentoService>();

        return services;
    }
}
