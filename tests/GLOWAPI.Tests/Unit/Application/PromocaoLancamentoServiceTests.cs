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
    public async Task ObterStatusAsync_DeveRetornarDisponivel_QuandoHouverVagas()
    {
        _campanhaRepository
            .Setup(r => r.ObterAtivaPorCodigoAsync(PromocaoLancamentoService.CodigoCampanhaLancamento, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CampanhaPromocional
            {
                Codigo = PromocaoLancamentoService.CodigoCampanhaLancamento,
                Limite = 100,
                Utilizados = 40,
                DiasTrial = 30,
                Ativa = true
            });

        var service = CreateService();
        var status = await service.ObterStatusAsync();

        Assert.True(status.Disponivel);
        Assert.Equal(60, status.VagasRestantes);
        Assert.Equal(30, status.DiasTrial);
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
        Assert.Equal(30, status.DiasTrial);
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

    private PromocaoLancamentoService CreateService() =>
        new(
            _campanhaRepository.Object,
            _assinaturaRepository.Object,
            Options.Create(new AssinaturaCobrancaOptions()));
}
