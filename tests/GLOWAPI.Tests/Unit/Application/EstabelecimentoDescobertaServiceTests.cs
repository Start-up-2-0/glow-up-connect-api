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
    private readonly Mock<IGeocodificadorService> _geocodificadorService = new();

    [Fact]
    public async Task ListarProximosAsync_DeveLancarExcecao_QuandoLatitudeInvalida()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<LocalizacaoClienteInvalidaException>(() =>
            service.ListarProximosAsync(95m, -47m, 10, 1, 20));
    }

    [Fact]
    public async Task ListarProximosAsync_DeveRetornarEstabelecimentos_QuandoReverseGeocodeFuncionar()
    {
        _geocodificadorService
            .Setup(g => g.ReverseGeocodificarAsync(-22.9056m, -47.0608m, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LocalizacaoReversa("Campinas", "SP"));

        var estabelecimento = new Estabelecimento
        {
            PublicGuid = Guid.NewGuid(),
            Nome = "Barbearia Glow",
            Descricao = "Corte premium",
            Logo = "logo.png",
            Ativo = true,
            Endereco = new Endereco
            {
                Logradouro = "Rua A",
                Bairro = "Centro",
                Cidade = "Campinas",
                Estado = "SP",
                Latitude = -22.906m,
                Longitude = -47.061m
            }
        };

        _estabelecimentoRepository
            .Setup(r => r.ListarProximosAsync(
                "campinas",
                "SP",
                -22.9056m,
                -47.0608m,
                10d,
                1,
                20,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<EstabelecimentoProximoConsulta>
            {
                new(estabelecimento, 1.2d)
            }, 1));

        var service = CreateService();
        var resultado = await service.ListarProximosAsync(-22.9056m, -47.0608m, 10, 1, 20);

        Assert.Equal("Campinas", resultado.Cidade);
        Assert.Equal("SP", resultado.Estado);
        Assert.Equal(1, resultado.Total);
        Assert.Single(resultado.Itens);
        Assert.Equal("Barbearia Glow", resultado.Itens[0].Nome);
        Assert.Equal(1.2d, resultado.Itens[0].DistanciaKm);
    }

    private EstabelecimentoDescobertaService CreateService() =>
        new(_estabelecimentoRepository.Object, _geocodificadorService.Object);
}
