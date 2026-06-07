using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
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
    private readonly Mock<IGatewayPagamentoResolver> _gatewayResolver = new();
    private readonly Mock<IGatewayPagamento> _gateway = new();
    private readonly Mock<IAssinaturaHistoricoService> _historicoService = new();
    private readonly Mock<IAssinaturaNotificacaoService> _notificacaoService = new();
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
                MetodoPagamento: "visa"));

        _gatewayResolver
            .Setup(r => r.Resolver(GatewayPagamento.MercadoPago))
            .Returns(_gateway.Object);

        _pagamentoRepository
            .Setup(r => r.ListarPorAssinaturaAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Pagamento>());
    }

    [Fact]
    public async Task GerarCobrancaRecorrenteAsync_DeveCriarPagamentoComCicloCorreto()
    {
        var assinatura = new Assinatura
        {
            Id = 10,
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

        var service = CreateService();
        var pagamento = await service.GerarCobrancaRecorrenteAsync(assinatura);

        Assert.NotNull(pagamentoGerado);
        Assert.Equal(TipoCobrancaAssinatura.Recorrente, pagamento.TipoCobranca);
        Assert.Equal(29.99m, pagamento.Valor);
        Assert.Equal(PagamentoStatus.Pendente, pagamento.Status);
        Assert.Equal(new DateTime(2026, 8, 15, 0, 0, 0, DateTimeKind.Utc), pagamento.DataVencimento);
        Assert.Equal("sub-sub-test-1-ciclo-1", pagamento.GatewayPaymentId);
        Assert.Equal("subscription", pagamento.MetodoPagamento);
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

    private CobrancaAssinaturaService CreateService() =>
        new(
            _pagamentoRepository.Object,
            _assinaturaRepository.Object,
            new Mock<IEstabelecimentoUsuarioRepository>().Object,
            _gatewayResolver.Object,
            _historicoService.Object,
            _notificacaoService.Object,
            new CicloCobrancaAssinaturaService(Options.Create(new AssinaturaCobrancaOptions())),
            _currentUser.Object);
}
