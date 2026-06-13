using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Options;
using GLOWAPI.Infrastructure.Database;
using GLOWAPI.Infrastructure.Geolocalizacao;
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
        var provider = DatabaseConnectionResolver.ResolveProvider(configuration);
        var connectionString = DatabaseConnectionResolver.ResolveConnectionString(configuration, environmentName);

        if (!provider.Equals(DatabaseConnectionResolver.ProviderMySql, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("O provedor de banco configurado para o projeto deve ser MySQL.");
        }

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string do MySQL nao configurada. Use ConnectionStrings:DefaultConnection em desenvolvimento ou a variavel de ambiente MYSQL_CS em staging/producao.");
        }

        var serverVersion = ServerVersion.Parse("8.0.36-mysql");

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseMySql(connectionString, serverVersion);
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
        services.AddScoped<IConviteNegocioRepository, ConviteNegocioRepository>();
        services.AddScoped<IAgendamentoRepository, AgendamentoRepository>();
        services.AddScoped<IAgendamentoItemRepository, AgendamentoItemRepository>();
        services.AddScoped<IAgendamentoHistoricoRepository, AgendamentoHistoricoRepository>();
        services.AddScoped<IAgendamentoPropostaRemarcacaoRepository, AgendamentoPropostaRemarcacaoRepository>();
        services.AddScoped<ICaixaRepository, CaixaRepository>();
        services.AddScoped<IPlanoRepository, PlanoRepository>();
        services.AddScoped<IAssinaturaRepository, AssinaturaRepository>();
        services.AddScoped<ICampanhaPromocionalRepository, CampanhaPromocionalRepository>();
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

        services.Configure<MercadoPagoOptions>(configuration.GetSection(MercadoPagoOptions.SectionName));
        services.Configure<GeocodificacaoOptions>(configuration.GetSection(GeocodificacaoOptions.SectionName));

        services.AddHttpClient<ResendClient>();
        services.Configure<ResendClientOptions>(options =>
        {
            options.ApiToken = configuration["RESEND_APITOKEN"] ?? string.Empty;
        });
        services.AddTransient<IResend, ResendClient>();
        services.Configure<MensageriaWhatsAppOptions>(configuration.GetSection(MensageriaWhatsAppOptions.SectionName));
        services.AddHttpClient<ProvedorMensagemWhatsApp>();
        services.AddScoped<IProvedorMensagem, ProvedorMensagemEmail>();
        services.AddScoped<IProvedorMensagem>(sp => sp.GetRequiredService<ProvedorMensagemWhatsApp>());
        services.AddScoped<IProvedorMensagem, ProvedorMensagemSms>();

        var mercadoPagoAccessToken = configuration[$"{MercadoPagoOptions.SectionName}:AccessToken"];
        if (string.IsNullOrWhiteSpace(mercadoPagoAccessToken))
        {
            services.AddScoped<IGatewayPagamento>(_ => new GatewayPagamentoFake(GLOWAPI.Domain.Enums.GatewayPagamento.MercadoPago));
        }
        else
        {
            services.AddHttpClient<GatewayPagamentoMercadoPago>((serviceProvider, client) =>
            {
                var options = serviceProvider
                    .GetRequiredService<Microsoft.Extensions.Options.IOptions<MercadoPagoOptions>>()
                    .Value;
                ConfigurarMercadoPagoHttpClient(client, options);
            });
            services.AddScoped<IGatewayPagamento>(sp => sp.GetRequiredService<GatewayPagamentoMercadoPago>());
        }

        services.AddScoped<IGatewayPagamento>(_ => new GatewayPagamentoFake(GLOWAPI.Domain.Enums.GatewayPagamento.AbacatePay));

        services.AddHttpClient<IGeocodificadorService, NominatimGeocodificadorClient>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<GeocodificacaoOptions>>().Value;
            var baseUrl = string.IsNullOrWhiteSpace(options.BaseUrl)
                ? "https://nominatim.openstreetmap.org"
                : options.BaseUrl.Trim();
            client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSegundos);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(options.UserAgent);
        });

        return services;
    }

    private static void ConfigurarMercadoPagoHttpClient(HttpClient client, MercadoPagoOptions options)
    {
        var apiBaseUrl = string.IsNullOrWhiteSpace(options.ApiBaseUrl)
            ? "https://api.mercadopago.com"
            : options.ApiBaseUrl.Trim();
        client.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/");
        client.Timeout = TimeSpan.FromSeconds(30);
    }
}
