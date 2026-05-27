using GLOWAPI.Application.DTOs.Pagamentos;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Pagamentos;
using Moq;

namespace GLOWAPI.Tests.Unit.Application;

public class WebhookPagamentoServiceTests
{
    private readonly Mock<IWebhookPagamentoRepository> _repository = new();
    private readonly Mock<IPagamentoRepository> _pagamentoRepository = new();

    [Fact]
    public async Task RegistrarAsync_DeveCriarWebhook_QuandoEventoNaoExiste()
    {
        _repository
            .Setup(r => r.ObterPorEventoAsync(GatewayPagamento.MercadoPago, "evt-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((WebhookPagamento?)null);

        WebhookPagamento? webhookCriado = null;
        _repository
            .Setup(r => r.AdicionarAsync(It.IsAny<WebhookPagamento>(), It.IsAny<CancellationToken>()))
            .Callback<WebhookPagamento, CancellationToken>((webhook, _) =>
            {
                webhook.Id = 10;
                webhookCriado = webhook;
            })
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var response = await service.RegistrarAsync(new RegistrarWebhookPagamentoRequestDto
        {
            Gateway = GatewayPagamento.MercadoPago,
            EventId = " evt-1 ",
            EventType = " payment.approved ",
            Payload = """{"id":"pay-1"}"""
        });

        Assert.Equal(10, response.Id);
        Assert.False(response.Duplicado);
        Assert.False(response.Processado);
        Assert.Equal("evt-1", response.EventId);
        Assert.Equal("payment.approved", response.EventType);

        Assert.NotNull(webhookCriado);
        Assert.Equal("""{"id":"pay-1"}""", webhookCriado!.Payload);
        Assert.Equal("Pagamento nao encontrado para o gatewayPaymentId informado.", webhookCriado.ErroProcessamento);

        _repository.Verify(r => r.AdicionarAsync(It.IsAny<WebhookPagamento>(), It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(r => r.SalvarAlteracoesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegistrarAsync_DeveRetornarDuplicado_QuandoEventoJaExiste()
    {
        var webhookExistente = new WebhookPagamento
        {
            Id = 5,
            Gateway = GatewayPagamento.AbacatePay,
            EventId = "evt-1",
            EventType = "billing.paid",
            Payload = "{}"
        };

        _repository
            .Setup(r => r.ObterPorEventoAsync(GatewayPagamento.AbacatePay, "evt-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(webhookExistente);

        var service = CreateService();

        var response = await service.RegistrarAsync(new RegistrarWebhookPagamentoRequestDto
        {
            Gateway = GatewayPagamento.AbacatePay,
            EventId = "evt-1",
            EventType = "billing.paid",
            Payload = "{}"
        });

        Assert.True(response.Duplicado);
        Assert.Equal(5, response.Id);

        _repository.Verify(r => r.AdicionarAsync(It.IsAny<WebhookPagamento>(), It.IsAny<CancellationToken>()), Times.Never);
        _repository.Verify(r => r.SalvarAlteracoesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RegistrarAsync_DeveLancarExcecao_QuandoEventIdNaoForInformado()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<WebhookPagamentoInvalidoException>(() =>
            service.RegistrarAsync(new RegistrarWebhookPagamentoRequestDto
            {
                Gateway = GatewayPagamento.MercadoPago,
                EventId = " ",
                EventType = "payment.approved",
                Payload = "{}"
            }));
    }

    [Fact]
    public async Task RegistrarAsync_DeveAtivarAssinatura_QuandoEventoForPagamentoAprovado()
    {
        var assinatura = new Assinatura
        {
            Id = 20,
            Status = AssinaturaStatus.PendentePagamento,
            Plano = new Plano
            {
                Id = 1,
                Periodo = PlanoPeriodo.Mensal
            }
        };

        var pagamento = new Pagamento
        {
            Id = 30,
            Gateway = GatewayPagamento.MercadoPago,
            GatewayPaymentId = "pay-1",
            Status = PagamentoStatus.Pendente,
            Assinatura = assinatura
        };

        _repository
            .Setup(r => r.ObterPorEventoAsync(GatewayPagamento.MercadoPago, "evt-aprovado", It.IsAny<CancellationToken>()))
            .ReturnsAsync((WebhookPagamento?)null);

        _repository
            .Setup(r => r.AdicionarAsync(It.IsAny<WebhookPagamento>(), It.IsAny<CancellationToken>()))
            .Callback<WebhookPagamento, CancellationToken>((webhook, _) => webhook.Id = 10)
            .Returns(Task.CompletedTask);

        _pagamentoRepository
            .Setup(r => r.ObterPorGatewayPaymentIdAsync(GatewayPagamento.MercadoPago, "pay-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagamento);

        var service = CreateService();

        var response = await service.RegistrarAsync(new RegistrarWebhookPagamentoRequestDto
        {
            Gateway = GatewayPagamento.MercadoPago,
            EventId = "evt-aprovado",
            EventType = "payment.approved",
            Payload = """{"gatewayPaymentId":"pay-1"}"""
        });

        Assert.True(response.Processado);
        Assert.Equal(PagamentoStatus.Pago, pagamento.Status);
        Assert.NotNull(pagamento.PagoEm);
        Assert.Equal(AssinaturaStatus.Ativa, assinatura.Status);
        Assert.Equal(30, assinatura.UltimoPagamentoId);
        Assert.NotNull(assinatura.Fim);
        Assert.Equal(assinatura.Inicio.AddMonths(1), assinatura.Fim);

        _pagamentoRepository.Verify(r => r.Atualizar(pagamento), Times.Once);
        _repository.Verify(r => r.SalvarAlteracoesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private WebhookPagamentoService CreateService() =>
        new(_repository.Object, _pagamentoRepository.Object);
}
