using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Options;
using GLOWAPI.Application.Services;
using GLOWAPI.Domain.Entities;
using Microsoft.Extensions.Options;
using Moq;

namespace GLOWAPI.Tests.Unit.Application;

public class PromocaoLancamentoServiceTests
{
    private readonly Mock<ICampanhaPromocionalRepository> _campanhaRepository = new();
    private readonly Mock<IAssinaturaRepository> _assinaturaRepository = new();

    [Fact]
    public async Task ObterStatusAsync_DeveContarVagasPorAssinaturasDaCampanha()
    {
        _campanhaRepository
            .Setup(r => r.ObterAtivaPorCodigoAsync(PromocaoLancamentoService.CodigoCampanhaLancamento, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CampanhaPromocional
            {
                Codigo = PromocaoLancamentoService.CodigoCampanhaLancamento,
                Limite = 100,
                Utilizados = 0,
                DiasTrial = PromocaoLancamentoService.DiasTrialPadrao,
                PercentualDescontoMensalidade = 50,
                Ativa = true
            });
        _assinaturaRepository
            .Setup(r => r.ContarPorCodigoCampanhaAsync(PromocaoLancamentoService.CodigoCampanhaLancamento, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        var service = CreateService();
        var status = await service.ObterStatusAsync();

        Assert.True(status.Disponivel);
        Assert.Equal(98, status.VagasRestantes);
        Assert.Equal(PromocaoLancamentoService.DiasTrialPadrao, status.DiasTrial);
        Assert.Equal(50, status.PercentualDescontoMensalidade);
    }

    [Fact]
    public async Task ObterStatusAsync_DeveRetornarIndisponivel_QuandoAssinaturasAtingiremLimite()
    {
        _campanhaRepository
            .Setup(r => r.ObterAtivaPorCodigoAsync(PromocaoLancamentoService.CodigoCampanhaLancamento, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CampanhaPromocional
            {
                Codigo = PromocaoLancamentoService.CodigoCampanhaLancamento,
                Limite = 100,
                Utilizados = 40,
                DiasTrial = PromocaoLancamentoService.DiasTrialPadrao,
                PercentualDescontoMensalidade = 50,
                Ativa = true
            });
        _assinaturaRepository
            .Setup(r => r.ContarPorCodigoCampanhaAsync(PromocaoLancamentoService.CodigoCampanhaLancamento, It.IsAny<CancellationToken>()))
            .ReturnsAsync(100);

        var service = CreateService();
        var status = await service.ObterStatusAsync();

        Assert.False(status.Disponivel);
        Assert.Equal(0, status.VagasRestantes);
    }

    [Fact]
    public async Task ObterStatusAsync_DeveRetornarIndisponivel_QuandoCampanhaNaoExistir()
    {
        _campanhaRepository
            .Setup(r => r.ObterAtivaPorCodigoAsync(PromocaoLancamentoService.CodigoCampanhaLancamento, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CampanhaPromocional?)null);

        var service = CreateService();
        var status = await service.ObterStatusAsync();

        Assert.False(status.Disponivel);
        Assert.Equal(0, status.VagasRestantes);
        Assert.Equal(PromocaoLancamentoService.DiasTrialPadrao, status.DiasTrial);
        Assert.Equal(50, status.PercentualDescontoMensalidade);
    }

    [Fact]
    public async Task TentarReservarVagaAsync_DeveRetornarFalse_QuandoEstabelecimentoJaUsouPromocao()
    {
        _assinaturaRepository
            .Setup(r => r.ExisteComCampanhaPorEstabelecimentoAsync(10, PromocaoLancamentoService.CodigoCampanhaLancamento, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = CreateService();
        var reservou = await service.TentarReservarVagaAsync(10);

        Assert.False(reservou);
        _campanhaRepository.Verify(
            r => r.TentarReservarVagaAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task TentarReservarVagaAsync_DeveRetornarFalse_QuandoLimiteDeAssinaturasFoiAtingido()
    {
        _campanhaRepository
            .Setup(r => r.ObterAtivaPorCodigoAsync(PromocaoLancamentoService.CodigoCampanhaLancamento, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CampanhaPromocional
            {
                Codigo = PromocaoLancamentoService.CodigoCampanhaLancamento,
                Limite = 100,
                Ativa = true
            });
        _assinaturaRepository
            .Setup(r => r.ContarPorCodigoCampanhaAsync(PromocaoLancamentoService.CodigoCampanhaLancamento, It.IsAny<CancellationToken>()))
            .ReturnsAsync(100);

        var service = CreateService();
        var reservou = await service.TentarReservarVagaAsync(0);

        Assert.False(reservou);
        _campanhaRepository.Verify(
            r => r.TentarReservarVagaAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private PromocaoLancamentoService CreateService() =>
        new(
            _campanhaRepository.Object,
            _assinaturaRepository.Object,
            Options.Create(new AssinaturaCobrancaOptions()));
}
