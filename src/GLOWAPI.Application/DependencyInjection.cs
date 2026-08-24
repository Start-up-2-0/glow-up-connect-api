
using Microsoft.Extensions.DependencyInjection;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Options;
using GLOWAPI.Application.Services;
using GLOWAPI.Application.Validators;


namespace GLOWAPI.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IUsuarioService, UsuarioService>();
        services.AddScoped<IPrivacidadeTitularService, PrivacidadeTitularService>();
        services.AddScoped<IConfirmacaoEmailService, ConfirmacaoEmailService>();
        services.AddScoped<IRecuperacaoSenhaService, RecuperacaoSenhaService>();
        services.AddScoped<IConfirmacaoWhatsAppService, ConfirmacaoWhatsAppService>();
        services.AddScoped<IConfirmacaoWhatsAppEstabelecimentoService, ConfirmacaoWhatsAppEstabelecimentoService>();
        services.AddScoped<IConfirmacaoWhatsAppNotificacaoService, ConfirmacaoWhatsAppNotificacaoService>();
        services.AddScoped<IConfirmacaoWhatsAppInboundService, ConfirmacaoWhatsAppInboundService>();
        services.AddScoped<IAvatarBase64Decoder, AvatarBase64Decoder>();
        services.AddSingleton<IBase64ImageThumbnailer, Base64ImageThumbnailer>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IExclusaoContaService, ExclusaoContaService>();
        services.AddScoped<IAuthSessionService, AuthSessionService>();
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<ICurrentUserContext, CurrentUserContext>();
        services.AddScoped<IMensagemNotificacaoService, MensagemNotificacaoService>();
        services.AddScoped<IMensagemNotificacaoProcessadorService, MensagemNotificacaoProcessadorService>();
        services.AddScoped<IProvedorMensagemResolver, ProvedorMensagemResolver>();
        services.AddScoped<IPlanoService, PlanoService>();
        services.AddScoped<ICicloCobrancaAssinaturaService, CicloCobrancaAssinaturaService>();
        services.AddScoped<IPromocaoLancamentoService, PromocaoLancamentoService>();
        services.AddScoped<ICobrancaAssinaturaService, CobrancaAssinaturaService>();
        services.AddScoped<IAssinaturaCobrancaWorkerService, AssinaturaCobrancaWorkerService>();
        services.AddScoped<IAssinaturaVisibilidadeService, AssinaturaVisibilidadeService>();
        services.AddScoped<IAssinaturaEncerramentoService, AssinaturaEncerramentoService>();
        services.AddScoped<IAssinaturaService, AssinaturaService>();
        services.AddScoped<IAssinaturaOnboardingFinalizacaoService, AssinaturaOnboardingFinalizacaoService>();
        services.AddScoped<IAssinaturaOnboardingContextoService, AssinaturaOnboardingContextoService>();
        services.AddScoped<IOnboardingPublicacaoService, OnboardingPublicacaoService>();
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
        services.AddScoped<IMovimentacaoCaixaService, MovimentacaoCaixaService>();
        services.AddScoped<IRecebimentoAgendamentoService, RecebimentoAgendamentoService>();
        services.AddScoped<ISessaoCaixaNegocioService, SessaoCaixaNegocioService>();
        services.AddScoped<IFinanceiroNegocioService, FinanceiroNegocioService>();
        services.AddScoped<IMovimentosFinanceirosService, MovimentosFinanceirosService>();
        services.AddScoped<IProfissionalServicoNegocioService, ProfissionalServicoNegocioService>();
        services.AddScoped<IServicoNegocioService, ServicoNegocioService>();
        services.AddScoped<IServicoProfissionalAutonomoService, ServicoProfissionalAutonomoService>();
        services.AddScoped<IHorarioFuncionamentoNegocioService, HorarioFuncionamentoNegocioService>();
        services.AddScoped<IHorarioProfissionalNegocioService, HorarioProfissionalNegocioService>();
        services.AddScoped<IHorarioProfissionalAutonomoService, HorarioProfissionalAutonomoService>();
        services.AddScoped<IDisponibilidadeAgendaService, DisponibilidadeAgendaService>();
        services.AddScoped<IAgendamentoValidador, AgendamentoValidador>();
        services.AddScoped<IAgendamentoNegocioService, AgendamentoNegocioService>();
        services.AddScoped<IDashboardClienteService, DashboardClienteService>();
        services.AddScoped<IDashboardNegocioService, DashboardNegocioService>();
        services.AddScoped<IAgendamentoConfirmacaoContaService, AgendamentoConfirmacaoContaService>();
        services.AddScoped<IAgendamentoNotificacaoService, AgendamentoNotificacaoService>();
        services.AddScoped<IAuditoriaNegocioService, AuditoriaNegocioService>();
        services.AddScoped<IAuditoriaConsultaNegocioService, AuditoriaConsultaNegocioService>();
        services.AddScoped<IClienteNegocioService, ClienteNegocioService>();
        services.AddScoped<IRedeNegocioService, RedeNegocioService>();
        services.AddScoped<IEquipeNotificacaoService, EquipeNotificacaoService>();
        services.AddScoped<IUsuarioNegocioContextoService, UsuarioNegocioContextoService>();
        services.AddScoped<IConviteNegocioService, ConviteNegocioService>();
        services.AddScoped<IAssinaturaNotificacaoService, AssinaturaNotificacaoService>();
        services.AddScoped<IAssinaturaTitularContatoService, AssinaturaTitularContatoService>();
        services.AddScoped<IGatewayPagamentoResolver, GatewayPagamentoResolver>();
        services.AddScoped<IWebhookPagamentoService, WebhookPagamentoService>();
        services.AddScoped<IWebhookWhatsAppService, WebhookWhatsAppService>();
        services.AddScoped<IEnderecoGeocodificacaoService, EnderecoGeocodificacaoService>();
        services.AddScoped<IEstabelecimentoDescobertaService, EstabelecimentoDescobertaService>();
        services.AddScoped<IAvaliacaoAtendimentoService, AvaliacaoAtendimentoService>();
        services.AddScoped<IAvaliacaoResumoService, AvaliacaoResumoService>();
        services.AddScoped<IAvaliacaoAgregadoWorkerService, AvaliacaoAgregadoWorkerService>();
        services.AddScoped<IFavoritoClienteService, FavoritoClienteService>();
        services.AddScoped<IMetaNegocioService, MetaNegocioService>();
        services.AddScoped<ICompactacaoImagensPersistidasService, CompactacaoImagensPersistidasService>();
        services.AddScoped<IRetencaoDadosService, RetencaoDadosService>();

        return services;
    }
}
