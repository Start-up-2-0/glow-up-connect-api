using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using Moq;

namespace GLOWAPI.Tests.Unit.Application;

public class AssinaturaHistoricoServiceTests
{
    private readonly Mock<IRepository<AssinaturaHistorico>> _assinaturaHistoricoRepository = new();
    private readonly Mock<IRepository<PagamentoHistorico>> _pagamentoHistoricoRepository = new();
    private readonly Mock<IRepository<AssinaturaRecorrenciaHistorico>> _recorrenciaHistoricoRepository = new();

    [Fact]
    public async Task RegistrarAssinaturaAsync_DevePersistirEventoComStatusEPlano()
    {
        AssinaturaHistorico? historico = null;
        _assinaturaHistoricoRepository
            .Setup(r => r.AdicionarAsync(It.IsAny<AssinaturaHistorico>(), It.IsAny<CancellationToken>()))
            .Callback<AssinaturaHistorico, CancellationToken>((entity, _) => historico = entity)
            .Returns(Task.CompletedTask);

        var assinatura = new Assinatura
        {
            Id = 10,
            PlanoId = 2,
            Status = AssinaturaStatus.PendentePagamento
        };

        await CreateService().RegistrarAssinaturaAsync(
            assinatura,
            "AssinaturaIniciada",
            null,
            AssinaturaStatus.PendentePagamento,
            observacao: "Criada");

        Assert.NotNull(historico);
        Assert.Equal(10, historico!.AssinaturaId);
        Assert.Equal("AssinaturaIniciada", historico.Evento);
        Assert.Equal(AssinaturaStatus.PendentePagamento, historico.StatusNovo);
        Assert.Equal(2, historico.PlanoId);
        Assert.Equal("Criada", historico.Observacao);
    }

    [Fact]
    public async Task RegistrarPagamentoAsync_DevePersistirSnapshotDoPagamento()
    {
        PagamentoHistorico? historico = null;
        _pagamentoHistoricoRepository
            .Setup(r => r.AdicionarAsync(It.IsAny<PagamentoHistorico>(), It.IsAny<CancellationToken>()))
            .Callback<PagamentoHistorico, CancellationToken>((entity, _) => historico = entity)
            .Returns(Task.CompletedTask);

        var pagamento = new Pagamento
        {
            Id = 20,
            AssinaturaId = 10,
            Gateway = GatewayPagamento.MercadoPago,
            GatewayPaymentId = "pay-1",
            MetodoPagamento = "pix",
            Status = PagamentoStatus.Pago,
            Valor = 99.90m,
            Moeda = "BRL"
        };

        await CreateService().RegistrarPagamentoAsync(
            pagamento,
            "PagamentoAprovado",
            PagamentoStatus.Pendente,
            PagamentoStatus.Pago);

        Assert.NotNull(historico);
        Assert.Equal(20, historico!.PagamentoId);
        Assert.Equal(10, historico.AssinaturaId);
        Assert.Equal("pay-1", historico.GatewayPaymentId);
        Assert.Equal("pix", historico.MetodoPagamento);
        Assert.Equal(99.90m, historico.Valor);
    }

    [Fact]
    public async Task RegistrarRecorrenciaAsync_DevePersistirCicloDaAssinatura()
    {
        AssinaturaRecorrenciaHistorico? historico = null;
        _recorrenciaHistoricoRepository
            .Setup(r => r.AdicionarAsync(It.IsAny<AssinaturaRecorrenciaHistorico>(), It.IsAny<CancellationToken>()))
            .Callback<AssinaturaRecorrenciaHistorico, CancellationToken>((entity, _) => historico = entity)
            .Returns(Task.CompletedTask);

        var inicio = DateTime.UtcNow;
        var fim = inicio.AddMonths(1);
        var assinatura = new Assinatura
        {
            Id = 10,
            RenovacaoAutomatica = true
        };

        await CreateService().RegistrarRecorrenciaAsync(
            assinatura,
            "RecorrenciaLiberada",
            "Ativa",
            cicloInicio: inicio,
            cicloFim: fim);

        Assert.NotNull(historico);
        Assert.Equal(10, historico!.AssinaturaId);
        Assert.Equal("RecorrenciaLiberada", historico.Evento);
        Assert.Equal("Ativa", historico.Status);
        Assert.Equal(inicio, historico.CicloInicio);
        Assert.Equal(fim, historico.CicloFim);
        Assert.True(historico.RenovacaoAutomatica);
    }

    private AssinaturaHistoricoService CreateService() =>
        new(
            _assinaturaHistoricoRepository.Object,
            _pagamentoHistoricoRepository.Object,
            _recorrenciaHistoricoRepository.Object);
}
