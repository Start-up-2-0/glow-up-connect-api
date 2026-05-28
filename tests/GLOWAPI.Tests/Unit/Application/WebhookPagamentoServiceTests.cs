using GLOWAPI.Application.DTOs.Pagamentos;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Models.Pagamentos;
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
    private readonly Mock<IAssinaturaRepository> _assinaturaRepository = new();
    private readonly Mock<IAssinaturaNotificacaoService> _assinaturaNotificacaoService = new();
    private readonly Mock<IGatewayPagamentoResolver> _gatewayPagamentoResolver = new();
    private readonly Mock<IGatewayPagamento> _gatewayPagamento = new();
    private readonly Mock<IAssinaturaHistoricoService> _assinaturaHistoricoService = new();

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
            Payload = """{"gatewayPaymentId":"pay-1","email":"cliente@email.com"}"""
        });

        Assert.True(response.Processado);
        Assert.Equal(PagamentoStatus.Pago, pagamento.Status);
        Assert.NotNull(pagamento.PagoEm);
        Assert.Equal(AssinaturaStatus.Ativa, assinatura.Status);
        Assert.Equal(30, assinatura.UltimoPagamentoId);
        Assert.NotNull(assinatura.Fim);
        Assert.Equal(assinatura.Inicio.AddMonths(1), assinatura.Fim);

        _pagamentoRepository.Verify(r => r.Atualizar(pagamento), Times.Once);
        _assinaturaNotificacaoService.Verify(n => n.PagamentoConfirmadoAsync(
            assinatura,
            pagamento,
            "cliente@email.com",
            It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(r => r.SalvarAlteracoesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegistrarAsync_DeveNotificarPagamentoRecusado_QuandoEventoForRecusa()
    {
        var assinatura = new Assinatura
        {
            Id = 20,
            Status = AssinaturaStatus.PendentePagamento
        };

        var pagamento = new Pagamento
        {
            Id = 30,
            Gateway = GatewayPagamento.MercadoPago,
            GatewayPaymentId = "pay-recusado",
            Status = PagamentoStatus.Pendente,
            Valor = 99.90m,
            Assinatura = assinatura
        };

        _repository
            .Setup(r => r.ObterPorEventoAsync(GatewayPagamento.MercadoPago, "evt-recusado", It.IsAny<CancellationToken>()))
            .ReturnsAsync((WebhookPagamento?)null);

        _repository
            .Setup(r => r.AdicionarAsync(It.IsAny<WebhookPagamento>(), It.IsAny<CancellationToken>()))
            .Callback<WebhookPagamento, CancellationToken>((webhook, _) => webhook.Id = 14)
            .Returns(Task.CompletedTask);

        _pagamentoRepository
            .Setup(r => r.ObterPorGatewayPaymentIdAsync(GatewayPagamento.MercadoPago, "pay-recusado", It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagamento);

        var service = CreateService();

        var response = await service.RegistrarAsync(new RegistrarWebhookPagamentoRequestDto
        {
            Gateway = GatewayPagamento.MercadoPago,
            EventId = "evt-recusado",
            EventType = "payment.rejected",
            Payload = """{"gatewayPaymentId":"pay-recusado","customerEmail":"cliente@email.com"}"""
        });

        Assert.True(response.Processado);
        Assert.Equal(PagamentoStatus.Recusado, pagamento.Status);

        _pagamentoRepository.Verify(r => r.Atualizar(pagamento), Times.Once);
        _assinaturaNotificacaoService.Verify(n => n.PagamentoRecusadoAsync(
            pagamento,
            "cliente@email.com",
            It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(r => r.SalvarAlteracoesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegistrarAsync_DeveAplicarPlanoPendente_QuandoPagamentoDaTrocaForAprovado()
    {
        var planoAtual = new Plano
        {
            Id = 1,
            Periodo = PlanoPeriodo.Mensal
        };

        var novoPlano = new Plano
        {
            Id = 2,
            Periodo = PlanoPeriodo.Anual
        };

        var assinatura = new Assinatura
        {
            Id = 20,
            PlanoId = 1,
            PlanoAlteracaoPendenteId = 2,
            Status = AssinaturaStatus.Ativa,
            Plano = planoAtual,
            PlanoAlteracaoPendente = novoPlano
        };

        var pagamento = new Pagamento
        {
            Id = 31,
            Gateway = GatewayPagamento.MercadoPago,
            GatewayPaymentId = "pay-troca",
            Status = PagamentoStatus.Pendente,
            Assinatura = assinatura
        };

        _repository
            .Setup(r => r.ObterPorEventoAsync(GatewayPagamento.MercadoPago, "evt-troca", It.IsAny<CancellationToken>()))
            .ReturnsAsync((WebhookPagamento?)null);

        _repository
            .Setup(r => r.AdicionarAsync(It.IsAny<WebhookPagamento>(), It.IsAny<CancellationToken>()))
            .Callback<WebhookPagamento, CancellationToken>((webhook, _) => webhook.Id = 11)
            .Returns(Task.CompletedTask);

        _pagamentoRepository
            .Setup(r => r.ObterPorGatewayPaymentIdAsync(GatewayPagamento.MercadoPago, "pay-troca", It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagamento);

        var service = CreateService();

        var response = await service.RegistrarAsync(new RegistrarWebhookPagamentoRequestDto
        {
            Gateway = GatewayPagamento.MercadoPago,
            EventId = "evt-troca",
            EventType = "payment.approved",
            Payload = """{"gatewayPaymentId":"pay-troca"}"""
        });

        Assert.True(response.Processado);
        Assert.Equal(PagamentoStatus.Pago, pagamento.Status);
        Assert.Equal(2, assinatura.PlanoId);
        Assert.Same(novoPlano, assinatura.Plano);
        Assert.Null(assinatura.PlanoAlteracaoPendenteId);
        Assert.Null(assinatura.PlanoAlteracaoPendente);
        Assert.Equal(31, assinatura.UltimoPagamentoId);
        Assert.NotNull(assinatura.Fim);
        Assert.Equal(assinatura.Inicio.AddYears(1), assinatura.Fim);

        _pagamentoRepository.Verify(r => r.Atualizar(pagamento), Times.Once);
        _repository.Verify(r => r.SalvarAlteracoesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegistrarAsync_DeveConsultarGateway_QuandoWebhookMercadoPagoEnviarPaymentUpdated()
    {
        var assinatura = new Assinatura
        {
            Id = 1,
            PlanoId = 2,
            Status = AssinaturaStatus.PendentePagamento,
            Plano = new Plano { Id = 2, Periodo = PlanoPeriodo.Mensal }
        };
        var pagamento = new Pagamento
        {
            Id = 30,
            AssinaturaId = 1,
            Assinatura = assinatura,
            Gateway = GatewayPagamento.MercadoPago,
            GatewayPaymentId = "123456",
            Status = PagamentoStatus.Pendente
        };

        _repository
            .Setup(r => r.ObterPorEventoAsync(GatewayPagamento.MercadoPago, "evt-mp-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((WebhookPagamento?)null);
        _repository
            .Setup(r => r.AdicionarAsync(It.IsAny<WebhookPagamento>(), It.IsAny<CancellationToken>()))
            .Callback<WebhookPagamento, CancellationToken>((webhook, _) => webhook.Id = 15)
            .Returns(Task.CompletedTask);
        _gatewayPagamentoResolver
            .Setup(r => r.Resolver(GatewayPagamento.MercadoPago))
            .Returns(_gatewayPagamento.Object);
        _gatewayPagamento
            .Setup(g => g.ConsultarPagamentoAsync("123456", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConsultarPagamentoGatewayResponse(
                Sucesso: true,
                GatewayPaymentId: "123456",
                Status: "approved",
                ResponsePayload: "{}",
                PagadorEmail: "cliente@email.com"));
        _pagamentoRepository
            .Setup(r => r.ObterPorGatewayPaymentIdAsync(GatewayPagamento.MercadoPago, "123456", It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagamento);

        var response = await CreateService().RegistrarAsync(new RegistrarWebhookPagamentoRequestDto
        {
            Gateway = GatewayPagamento.MercadoPago,
            EventId = "evt-mp-1",
            EventType = "payment.updated",
            Payload = """{"action":"payment.updated","data":{"id":"123456"}}"""
        });

        Assert.True(response.Processado);
        Assert.Equal(PagamentoStatus.Pago, pagamento.Status);
        Assert.Equal(AssinaturaStatus.Ativa, assinatura.Status);
        _gatewayPagamento.Verify(g => g.ConsultarPagamentoAsync("123456", It.IsAny<CancellationToken>()), Times.Once);
        _assinaturaNotificacaoService.Verify(n => n.PagamentoConfirmadoAsync(
            assinatura,
            pagamento,
            "cliente@email.com",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegistrarAsync_DeveCancelarAssinatura_QuandoEventoForCancelamento()
    {
        var assinatura = new Assinatura
        {
            Id = 20,
            Status = AssinaturaStatus.Ativa,
            RenovacaoAutomatica = true,
            PlanoAlteracaoPendenteId = 2
        };

        _repository
            .Setup(r => r.ObterPorEventoAsync(GatewayPagamento.MercadoPago, "evt-cancelado", It.IsAny<CancellationToken>()))
            .ReturnsAsync((WebhookPagamento?)null);

        _repository
            .Setup(r => r.AdicionarAsync(It.IsAny<WebhookPagamento>(), It.IsAny<CancellationToken>()))
            .Callback<WebhookPagamento, CancellationToken>((webhook, _) => webhook.Id = 12)
            .Returns(Task.CompletedTask);

        _assinaturaRepository
            .Setup(r => r.ObterPorIdComPlanoAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assinatura);

        var service = CreateService();

        var response = await service.RegistrarAsync(new RegistrarWebhookPagamentoRequestDto
        {
            Gateway = GatewayPagamento.MercadoPago,
            EventId = "evt-cancelado",
            EventType = "subscription.cancelled",
            Payload = """{"assinaturaId":20,"email":"cliente@email.com"}"""
        });

        Assert.True(response.Processado);
        Assert.Equal(AssinaturaStatus.Cancelada, assinatura.Status);
        Assert.NotNull(assinatura.CanceladoEm);
        Assert.False(assinatura.RenovacaoAutomatica);
        Assert.Null(assinatura.PlanoAlteracaoPendenteId);

        _assinaturaRepository.Verify(r => r.Atualizar(assinatura), Times.Once);
        _assinaturaNotificacaoService.Verify(n => n.AssinaturaCanceladaAsync(
            assinatura,
            "cliente@email.com",
            It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(r => r.SalvarAlteracoesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegistrarAsync_DeveSuspenderAssinatura_QuandoEventoForSuspensao()
    {
        var assinatura = new Assinatura
        {
            Id = 20,
            Status = AssinaturaStatus.Ativa,
            RenovacaoAutomatica = true
        };

        _repository
            .Setup(r => r.ObterPorEventoAsync(GatewayPagamento.MercadoPago, "evt-suspenso", It.IsAny<CancellationToken>()))
            .ReturnsAsync((WebhookPagamento?)null);

        _repository
            .Setup(r => r.AdicionarAsync(It.IsAny<WebhookPagamento>(), It.IsAny<CancellationToken>()))
            .Callback<WebhookPagamento, CancellationToken>((webhook, _) => webhook.Id = 13)
            .Returns(Task.CompletedTask);

        _assinaturaRepository
            .Setup(r => r.ObterPorIdComPlanoAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assinatura);

        var service = CreateService();

        var response = await service.RegistrarAsync(new RegistrarWebhookPagamentoRequestDto
        {
            Gateway = GatewayPagamento.MercadoPago,
            EventId = "evt-suspenso",
            EventType = "subscription.suspended",
            Payload = """{"data":{"assinaturaId":"20","email":"cliente@email.com"}}"""
        });

        Assert.True(response.Processado);
        Assert.Equal(AssinaturaStatus.Suspensa, assinatura.Status);
        Assert.Null(assinatura.CanceladoEm);
        Assert.False(assinatura.RenovacaoAutomatica);

        _assinaturaRepository.Verify(r => r.Atualizar(assinatura), Times.Once);
        _assinaturaNotificacaoService.Verify(n => n.AssinaturaSuspensaAsync(
            assinatura,
            "cliente@email.com",
            It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(r => r.SalvarAlteracoesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private WebhookPagamentoService CreateService() =>
        new(
            _repository.Object,
            _pagamentoRepository.Object,
            _assinaturaRepository.Object,
            _assinaturaNotificacaoService.Object,
            _gatewayPagamentoResolver.Object,
            _assinaturaHistoricoService.Object);
}
