using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Models.Geolocalizacao;
using GLOWAPI.Application.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Exceptions.Negocios;
using Moq;

namespace GLOWAPI.Tests.Unit.Application;

public class EstabelecimentoDescobertaServiceTests
{
    private readonly Mock<IEstabelecimentoRepository> _estabelecimentoRepository = new();
    private readonly Mock<IHorarioFuncionamentoEstabelecimentoRepository> _horarioFuncionamentoRepository = new();
    private readonly Mock<IGeocodificadorService> _geocodificadorService = new();
    private readonly Mock<IBase64ImageThumbnailer> _thumbnailer = new();

    [Fact]
    public async Task ListarProximosAsync_DeveLancarExcecao_QuandoLatitudeInvalida()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<LocalizacaoClienteInvalidaException>(() =>
            service.ListarProximosAsync(95m, -47m, 10, 1, 20, null));
    }

    [Fact]
    public async Task ListarProximosAsync_DeveRetornarEstabelecimentos_QuandoReverseGeocodeFuncionar()
    {
        _geocodificadorService
            .Setup(g => g.ReverseGeocodificarAsync(-22.9056m, -47.0608m, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LocalizacaoReversa("Campinas", "SP"));

        _thumbnailer
            .Setup(t => t.ParaListagem(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>()))
            .Returns((string? logo, int _, int _) => logo ?? string.Empty);

        var publicGuid = Guid.NewGuid();
        _estabelecimentoRepository
            .Setup(r => r.ListarProximosAsync(
                "campinas",
                "SP",
                -22.9056m,
                -47.0608m,
                10d,
                1,
                20,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<EstabelecimentoProximoConsulta>
            {
                new(
                    1,
                    publicGuid,
                    "Barbearia Glow",
                    "data:image/png;base64,abc",
                    "Corte premium",
                    null,
                    0,
                    null,
                    null,
                    "Rua A",
                    "Centro",
                    "Campinas",
                    "SP",
                    -22.906m,
                    -47.061m,
                    1.2d,
                    false,
                    Domain.Enums.TipoAssinatura.Estabelecimento)
            }, 1));

        var service = CreateService();
        var resultado = await service.ListarProximosAsync(-22.9056m, -47.0608m, 10, 1, 20, null);

        Assert.Equal("Campinas", resultado.Cidade);
        Assert.Equal("SP", resultado.Estado);
        Assert.Equal(1, resultado.Total);
        Assert.Single(resultado.Itens);
        Assert.Equal("Barbearia Glow", resultado.Itens[0].Nome);
        Assert.Equal(1.2d, resultado.Itens[0].DistanciaKm);
        Assert.Equal("data:image/png;base64,abc", resultado.Itens[0].Logo);
    }

    private EstabelecimentoDescobertaService CreateService()
    {
        _horarioFuncionamentoRepository
            .Setup(r => r.ListarPorEstabelecimentoAsync(
                It.IsAny<int>(),
                It.IsAny<DayOfWeek?>(),
                It.IsAny<bool?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<HorarioFuncionamentoEstabelecimento>());

        return new(
            _estabelecimentoRepository.Object,
            _horarioFuncionamentoRepository.Object,
            _geocodificadorService.Object,
            _thumbnailer.Object);
    }
}
