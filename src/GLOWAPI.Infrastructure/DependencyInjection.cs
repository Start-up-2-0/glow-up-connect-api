using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Options;
using GLOWAPI.Infrastructure.Mensageria.Provedores;
using GLOWAPI.Infrastructure.Pagamentos;
using GLOWAPI.Infrastructure.Repositories;
using GLOWAPI.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Resend;



namespace GLOWAPI.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        string environmentName)
    {
        var provider = configuration.GetValue<string>("Database:Provider") ?? "PostgreSQL";
        var isHosted = string.Equals(environmentName, "Production", StringComparison.OrdinalIgnoreCase)
            || string.Equals(environmentName, "Staging", StringComparison.OrdinalIgnoreCase);
        var connectionString = isHosted
            ? configuration["POSTGSL"]
            : configuration.GetConnectionString("DefaultConnection");

        if (!provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("O provedor de banco configurado para o projeto deve ser PostgreSQL.");
        }

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string do PostgreSQL nao configurada. Use ConnectionStrings:DefaultConnection em desenvolvimento ou a variavel de ambiente POSTGSL em staging/producao.");
        }

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<IEstabelecimentoRepository, EstabelecimentoRepository>();
        services.AddScoped<IEstabelecimentoUsuarioRepository, EstabelecimentoUsuarioRepository>();
        services.AddScoped<IProfissionalRepository, ProfissionalRepository>();
        services.AddScoped<IProfissionalEstabelecimentoRepository, ProfissionalEstabelecimentoRepository>();
        services.AddScoped<IEnderecoRepository, EnderecoRepository>();
        services.AddScoped<IServicoRepository, ServicoRepository>();
        services.AddScoped<IProfissionalServicoRepository, ProfissionalServicoRepository>();
        services.AddScoped<IHorarioFuncionamentoEstabelecimentoRepository, HorarioFuncionamentoEstabelecimentoRepository>();
        services.AddScoped<IHorarioAtendimentoProfissionalRepository, HorarioAtendimentoProfissionalRepository>();
        services.AddScoped<IAgendamentoRepository, AgendamentoRepository>();
        services.AddScoped<IAgendamentoItemRepository, AgendamentoItemRepository>();
        services.AddScoped<ICaixaRepository, CaixaRepository>();
        services.AddScoped<IPlanoRepository, PlanoRepository>();
        services.AddScoped<IAssinaturaRepository, AssinaturaRepository>();
        services.AddScoped<IPagamentoRepository, PagamentoRepository>();
        services.AddScoped<ILancamentoCaixaRepository, LancamentoCaixaRepository>();
        services.AddScoped<IWebhookPagamentoRepository, WebhookPagamentoRepository>();
        services.AddScoped<IComissaoProfissionalRepository, ComissaoProfissionalRepository>();
        services.AddScoped<IMetaProfissionalRepository, MetaProfissionalRepository>();
        services.AddScoped<ISessaoAutenticacaoRepository, SessaoAutenticacaoRepository>();
        services.AddScoped<ILogAutenticacaoRepository, LogAutenticacaoRepository>();
        services.AddScoped<IGlowTokenService, GlowTokenService>();
        services.AddScoped<ISecurityAuditLogger, SecurityAuditLogger>();
        services.AddScoped<IMensagemNotificacaoRepository, MensagemNotificacaoRepository>();

        services.AddHttpClient<ResendClient>();
        services.Configure<ResendClientOptions>(options =>
        {
            options.ApiToken = configuration["RESEND_APITOKEN"] ?? string.Empty;
        });
        services.AddTransient<IResend, ResendClient>();
        services.AddScoped<IProvedorMensagem, ProvedorMensagemEmail>();
        services.AddScoped<IProvedorMensagem, ProvedorMensagemWhatsApp>();
        services.AddScoped<IProvedorMensagem, ProvedorMensagemSms>();
        services.AddScoped<IGatewayPagamento>(_ => new GatewayPagamentoFake(GLOWAPI.Domain.Enums.GatewayPagamento.MercadoPago));
        services.AddScoped<IGatewayPagamento>(_ => new GatewayPagamentoFake(GLOWAPI.Domain.Enums.GatewayPagamento.AbacatePay));

        return services;
    }
}
