using GLOWAPI.Application.Helpers;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Models.Assinaturas;
using GLOWAPI.Application.Models.Pagamentos;
using GLOWAPI.Application.Options;
using GLOWAPI.Application.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using Microsoft.Extensions.Options;
using Moq;

namespace GLOWAPI.Tests.Unit.Application;

public class CobrancaAssinaturaServiceTests
{
    private readonly Mock<IPagamentoRepository> _pagamentoRepository = new();
    private readonly Mock<IAssinaturaRepository> _assinaturaRepository = new();
    private readonly Mock<IEstabelecimentoUsuarioRepository> _estabelecimentoUsuarioRepository = new();
    private readonly Mock<IGatewayPagamentoResolver> _gatewayResolver = new();
    private readonly Mock<IGatewayPagamento> _gateway = new();
    private readonly Mock<IAssinaturaHistoricoService> _historicoService = new();
    private readonly Mock<IAssinaturaNotificacaoService> _notificacaoService = new();
    private readonly Mock<IAssinaturaTitularContatoService> _titularContatoService = new();
    private readonly Mock<ICurrentUserContext> _currentUser = new();

    public CobrancaAssinaturaServiceTests()
    {
        _currentUser.Setup(c => c.Email).Returns("cliente@email.com");
        _gateway.Setup(g => g.GatewaySuportado).Returns(GatewayPagamento.MercadoPago);
        _gateway
            .Setup(g => g.CriarCobrancaAsync(It.IsAny<CriarCobrancaGatewayRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CriarCobrancaGatewayResponse(
                Sucesso: true,
                GatewayPaymentId: "pay-rec-1",
                CheckoutUrl: "https://checkout.test",
                QrCode: string.Empty,
                RequestPayload: "{}",
                ResponsePayload: "{}",
                MetodoPagamento: "checkout_pro"));

        _gatewayResolver
            .Setup(r => r.Resolver(GatewayPagamento.MercadoPago))
            .Returns(_gateway.Object);

        _pagamentoRepository
            .Setup(r => r.ListarPorAssinaturaAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Pagamento>());

        _titularContatoService
            .Setup(s => s.ResolverAsync(It.IsAny<Assinatura>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssinaturaTitularContato
            {
                Email = "dono@estabelecimento.com",
                TelefoneWhatsApp = "5511999999999",
                EstabelecimentoId = 1
            });
    }

    [Fact]
    public async Task GerarCobrancaRecorrenteAsync_ComCheckoutPro_DeveCriarPreferenceENotificarTitular()
    {
        var assinatura = new Assinatura
        {
            Id = 10,
            EstabelecimentoId = 1,
            DataReferenciaCiclo = new DateTime(2026, 7, 15, 0, 0, 0, DateTimeKind.Utc),
            DiaVencimento = 15,
            Gateway = GatewayPagamento.MercadoPago,
            GatewaySubscriptionId = "sub-test-1",
            ProximaDataVencimento = new DateTime(2026, 8, 15, 0, 0, 0, DateTimeKind.Utc),
            ProximaDataGeracaoCobranca = new DateTime(2026, 8, 13, 0, 0, 0, DateTimeKind.Utc),
            ProximaDataAlerta = new DateTime(2026, 8, 12, 0, 0, 0, DateTimeKind.Utc),
            Plano = new Plano { Id = 1, Nome = "Basic", Preco = 29.99m }
        };

        Pagamento? pagamentoGerado = null;
        _pagamentoRepository
            .Setup(r => r.AdicionarAsync(It.IsAny<Pagamento>(), It.IsAny<CancellationToken>()))
            .Callback<Pagamento, CancellationToken>((pagamento, _) =>
            {
                pagamento.Id = 99;
                pagamentoGerado = pagamento;
            })
            .Returns(Task.CompletedTask);

        var service = CreateService(usarCheckoutPro: true);
        var pagamento = await service.GerarCobrancaRecorrenteAsync(assinatura);

        Assert.NotNull(pagamentoGerado);
        Assert.Equal(TipoCobrancaAssinatura.Recorrente, pagamento.TipoCobranca);
        Assert.Equal(29.99m, pagamento.Valor);
        Assert.Equal(PagamentoStatus.Pendente, pagamento.Status);
        Assert.Equal("pay-rec-1", pagamento.GatewayPaymentId);
        Assert.Equal("checkout_pro", pagamento.MetodoPagamento);

        _gateway.Verify(
            g => g.CriarCobrancaAsync(It.IsAny<CriarCobrancaGatewayRequest>(), It.IsAny<CancellationToken>()),
            Times.Once);

        _notificacaoService.Verify(
            s => s.CobrancaPendenteComLinkAsync(
                assinatura,
                pagamento,
                "https://checkout.test",
                It.IsAny<AssinaturaTitularContato>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GerarCobrancaRecorrenteAsync_SemCheckoutProComSubscription_DeveCriarPagamentoInterno()
    {
        var assinatura = new Assinatura
        {
            Id = 10,
            DataReferenciaCiclo = new DateTime(2026, 7, 15, 0, 0, 0, DateTimeKind.Utc),
            DiaVencimento = 15,
            Gateway = GatewayPagamento.MercadoPago,
            GatewaySubscriptionId = "sub-test-1",
            ProximaDataVencimento = new DateTime(2026, 8, 15, 0, 0, 0, DateTimeKind.Utc),
            ProximaDataGeracaoCobranca = new DateTime(2026, 8, 13, 0, 0, 0, DateTimeKind.Utc),
            ProximaDataAlerta = new DateTime(2026, 8, 12, 0, 0, 0, DateTimeKind.Utc),
            Plano = new Plano { Id = 1, Nome = "Basic", Preco = 29.99m }
        };

        Pagamento? pagamentoGerado = null;
        _pagamentoRepository
            .Setup(r => r.AdicionarAsync(It.IsAny<Pagamento>(), It.IsAny<CancellationToken>()))
            .Callback<Pagamento, CancellationToken>((pagamento, _) =>
            {
                pagamento.Id = 99;
                pagamentoGerado = pagamento;
            })
            .Returns(Task.CompletedTask);

        var service = CreateService(usarCheckoutPro: false);
        var pagamento = await service.GerarCobrancaRecorrenteAsync(assinatura);

        Assert.NotNull(pagamentoGerado);
        Assert.Equal("sub-sub-test-1-ciclo-1", pagamento.GatewayPaymentId);
        Assert.Equal("subscription", pagamento.MetodoPagamento);

        _gateway.Verify(
            g => g.CriarCobrancaAsync(It.IsAny<CriarCobrancaGatewayRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _notificacaoService.Verify(
            s => s.CobrancaPendenteComLinkAsync(
                It.IsAny<Assinatura>(),
                It.IsAny<Pagamento>(),
                It.IsAny<string>(),
                It.IsAny<AssinaturaTitularContato>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task MarcarAtrasadasAsync_DeveAtualizarPagamentosPendentesVencidos()
    {
        var pagamento = new Pagamento
        {
            Id = 1,
            Status = PagamentoStatus.Pendente,
            DataVencimento = DateTime.UtcNow.AddDays(-2),
            Assinatura = new Assinatura { Id = 10 }
        };

        _pagamentoRepository
            .Setup(r => r.ListarPendentesVencidosAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Pagamento> { pagamento });

        var service = CreateService();
        var marcados = await service.MarcarAtrasadasAsync();

        Assert.Equal(1, marcados);
        Assert.Equal(PagamentoStatus.Atrasado, pagamento.Status);
    }

    [Fact]
    public async Task GerarCobrancaInicialAsync_DevePreencherExpiraEmNoHorarioBrasil()
    {
        CriarCobrancaGatewayRequest? enviado = null;
        _gateway
            .Setup(g => g.CriarCobrancaAsync(It.IsAny<CriarCobrancaGatewayRequest>(), It.IsAny<CancellationToken>()))
            .Callback<CriarCobrancaGatewayRequest, CancellationToken>((request, _) => enviado = request)
            .ReturnsAsync(new CriarCobrancaGatewayResponse(
                Sucesso: true,
                GatewayPaymentId: "pref-exp",
                CheckoutUrl: "https://checkout.test",
                QrCode: string.Empty,
                RequestPayload: "{}",
                ResponsePayload: "{}",
                MetodoPagamento: "checkout_pro"));

        var assinatura = new Assinatura
        {
            DataReferenciaCiclo = DateTime.UtcNow,
            Gateway = GatewayPagamento.MercadoPago,
            TipoAssinatura = TipoAssinatura.Estabelecimento
        };
        var plano = new Plano { Id = 1, Nome = "Premium", Preco = 99.90m, Periodo = PlanoPeriodo.Mensal, Ativo = true };

        var service = CreateService();
        var resultado = await service.GerarCobrancaInicialAsync(assinatura, plano, null);
        var agoraBrasil = BrasilDateTimeHelper.Agora();

        Assert.NotNull(resultado.Pagamento.ExpiraEm);
        Assert.True(resultado.Pagamento.ExpiraEm >= agoraBrasil.AddMinutes(4));
        Assert.True(resultado.Pagamento.ExpiraEm <= agoraBrasil.AddMinutes(6));
        Assert.NotNull(enviado?.ExpiraEm);
        Assert.Equal(resultado.Pagamento.ExpiraEm, enviado!.ExpiraEm);
    }

    [Fact]
    public async Task GerarCobrancaInicialAsync_EmSandbox_DeveUsarExpiracaoMinimaDeTrintaMinutos()
    {
        CriarCobrancaGatewayRequest? enviado = null;
        _gateway
            .Setup(g => g.CriarCobrancaAsync(It.IsAny<CriarCobrancaGatewayRequest>(), It.IsAny<CancellationToken>()))
            .Callback<CriarCobrancaGatewayRequest, CancellationToken>((request, _) => enviado = request)
            .ReturnsAsync(new CriarCobrancaGatewayResponse(
                Sucesso: true,
                GatewayPaymentId: "pref-sandbox-exp",
                CheckoutUrl: "https://sandbox.mercadopago.com.br/checkout",
                QrCode: string.Empty,
                RequestPayload: "{}",
                ResponsePayload: "{}",
                MetodoPagamento: "checkout_pro"));

        var assinatura = new Assinatura
        {
            DataReferenciaCiclo = DateTime.UtcNow,
            Gateway = GatewayPagamento.MercadoPago,
            TipoAssinatura = TipoAssinatura.Estabelecimento
        };
        var plano = new Plano { Id = 1, Nome = "Premium", Preco = 99.90m, Periodo = PlanoPeriodo.Mensal, Ativo = true };

        var service = CreateService(usarSandbox: true);
        var resultado = await service.GerarCobrancaInicialAsync(assinatura, plano, null);
        var agoraBrasil = BrasilDateTimeHelper.Agora();

        Assert.NotNull(resultado.Pagamento.ExpiraEm);
        Assert.True(resultado.Pagamento.ExpiraEm >= agoraBrasil.AddMinutes(29));
        Assert.True(resultado.Pagamento.ExpiraEm <= agoraBrasil.AddMinutes(31));
        Assert.Equal(resultado.Pagamento.ExpiraEm, enviado!.ExpiraEm);
    }

    [Fact]
    public async Task ObterOuRenovarCheckoutInicialAsync_DeveReutilizarLinkValido()
    {
        var assinatura = new Assinatura
        {
            Id = 10,
            DataReferenciaCiclo = DateTime.UtcNow,
            Gateway = GatewayPagamento.MercadoPago,
            Plano = new Plano { Id = 1, Nome = "Premium", Preco = 99.90m }
        };
        var pendente = new Pagamento
        {
            Id = 5,
            AssinaturaId = 10,
            Status = PagamentoStatus.Pendente,
            TipoCobranca = TipoCobrancaAssinatura.Inicial,
            MetodoPagamento = "checkout_pro",
            GatewayPaymentId = "pref-ainda-valida",
            ExpiraEm = BrasilDateTimeHelper.Agora().AddMinutes(3)
        };

        _pagamentoRepository
            .Setup(r => r.ObterUltimoPendenteInicialPorAssinaturaAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pendente);

        var service = CreateService();
        var resultado = await service.ObterOuRenovarCheckoutInicialAsync(
            assinatura,
            assinatura.Plano!,
            null);

        Assert.False(resultado.Novo);
        Assert.Equal(pendente, resultado.Pagamento);
        Assert.Contains("pref-ainda-valida", resultado.CheckoutUrl);
        _gateway.Verify(
            g => g.CriarCobrancaAsync(It.IsAny<CriarCobrancaGatewayRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ObterOuRenovarCheckoutInicialAsync_DeveGerarNovoLink_QuandoExpirado()
    {
        var assinatura = new Assinatura
        {
            Id = 10,
            DataReferenciaCiclo = DateTime.UtcNow,
            Gateway = GatewayPagamento.MercadoPago,
            TipoAssinatura = TipoAssinatura.Estabelecimento
        };
        var plano = new Plano { Id = 1, Nome = "Premium", Preco = 99.90m, Periodo = PlanoPeriodo.Mensal };
        var expirado = new Pagamento
        {
            Id = 5,
            AssinaturaId = 10,
            Status = PagamentoStatus.Pendente,
            TipoCobranca = TipoCobrancaAssinatura.Inicial,
            ExpiraEm = BrasilDateTimeHelper.Agora().AddMinutes(-1)
        };

        _pagamentoRepository
            .Setup(r => r.ObterUltimoPendenteInicialPorAssinaturaAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expirado);

        var service = CreateService();
        var resultado = await service.ObterOuRenovarCheckoutInicialAsync(assinatura, plano, null);

        Assert.True(resultado.Novo);
        Assert.Equal(PagamentoStatus.Expirado, expirado.Status);
        Assert.Equal("pay-rec-1", resultado.Pagamento.GatewayPaymentId);
        _gateway.Verify(
            g => g.CriarCobrancaAsync(It.IsAny<CriarCobrancaGatewayRequest>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessarPagamentoAprovadoAsync_DeveAtivar_QuandoExpiraEmJaPassou()
    {
        var assinatura = new Assinatura
        {
            Id = 10,
            Status = AssinaturaStatus.PendentePagamento,
            Plano = new Plano { Id = 1, Periodo = PlanoPeriodo.Mensal }
        };
        var pagamento = new Pagamento
        {
            Id = 7,
            Status = PagamentoStatus.Pendente,
            Assinatura = assinatura,
            AssinaturaId = 10,
            ExpiraEm = BrasilDateTimeHelper.Agora().AddMinutes(-10)
        };

        var service = CreateService();
        await service.ProcessarPagamentoAprovadoAsync(pagamento, "{}");

        Assert.Equal(PagamentoStatus.Pago, pagamento.Status);
        Assert.Equal(AssinaturaStatus.Ativa, assinatura.Status);
    }

    private CobrancaAssinaturaService CreateService(bool usarCheckoutPro = true, bool usarSandbox = false) =>
        new(
            _pagamentoRepository.Object,
            _assinaturaRepository.Object,
            _estabelecimentoUsuarioRepository.Object,
            _gatewayResolver.Object,
            _historicoService.Object,
            _notificacaoService.Object,
            _titularContatoService.Object,
            new CicloCobrancaAssinaturaService(Options.Create(new AssinaturaCobrancaOptions())),
            _currentUser.Object,
            new Mock<IAssinaturaOnboardingFinalizacaoService>().Object,
            new Mock<IAssinaturaVisibilidadeService>().Object,
            new Mock<IOnboardingPublicacaoService>().Object,
            new Mock<IAssinaturaEncerramentoService>().Object,
            new Mock<IUsuarioRepository>().Object,
            Options.Create(new MercadoPagoOptions { UsarCheckoutPro = usarCheckoutPro, UsarSandbox = usarSandbox }),
            Options.Create(new AssinaturaCobrancaOptions()));
}
