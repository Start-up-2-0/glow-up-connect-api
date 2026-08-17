using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Models.Manutencao;
using GLOWAPI.Application.Options;
using GLOWAPI.Application.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace GLOWAPI.Tests.Unit.Application;

public class RetencaoDadosServiceTests
{
    [Fact]
    public async Task ExecutarAsync_DeveAplicarRetencaoDe30DiasEAgregarResultados()
    {
        var repository = new Mock<IRetencaoDadosRepository>();
        repository
            .Setup(r => r.RemoverMensagensNotificacaoLogsAntesDeAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        repository
            .Setup(r => r.RemoverMensagensNotificacaoTerminaisAntesDeAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);
        repository
            .Setup(r => r.RemoverWebhooksProcessadosAntesDeAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        repository
            .Setup(r => r.RemoverLogsAutenticacaoAntesDeAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(4);
        repository
            .Setup(r => r.RemoverSessoesAutenticacaoExpiradasAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);
        repository
            .Setup(r => r.AnularPayloadAssinaturasHistoricoAntesDeAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(6);
        repository
            .Setup(r => r.AnularPayloadPagamentosHistoricoAntesDeAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(7);
        repository
            .Setup(r => r.AnularPayloadAgendamentosHistoricoAntesDeAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(8);

        var antes = DateTime.UtcNow;
        var service = new RetencaoDadosService(
            repository.Object,
            Options.Create(new RetencaoDadosOptions { DiasRetencao = 30 }),
            NullLogger<RetencaoDadosService>.Instance);

        var resultado = await service.ExecutarAsync();
        var depois = DateTime.UtcNow;

        Assert.Equal(1, resultado.WebhooksRemovidos);
        Assert.Equal(2, resultado.MensagensNotificacaoLogsRemovidos);
        Assert.Equal(3, resultado.MensagensNotificacaoRemovidas);
        Assert.Equal(4, resultado.LogsAutenticacaoRemovidos);
        Assert.Equal(5, resultado.SessoesAutenticacaoRemovidas);
        Assert.Equal(6, resultado.AssinaturasHistoricoPayloadsAnulados);
        Assert.Equal(7, resultado.PagamentosHistoricoPayloadsAnulados);
        Assert.Equal(8, resultado.AgendamentosHistoricoPayloadsAnulados);
        Assert.Equal(36, resultado.TotalAfetados);

        repository.Verify(
            r => r.RemoverMensagensNotificacaoLogsAntesDeAsync(
                It.Is<DateTime>(d => d >= antes.AddDays(-31) && d <= depois.AddDays(-29)),
                It.IsAny<CancellationToken>()),
            Times.Once);
        repository.Verify(
            r => r.RemoverMensagensNotificacaoTerminaisAntesDeAsync(
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
