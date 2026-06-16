using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Models.Geolocalizacao;
using GLOWAPI.Application.Models.Geolocalizacao;
using GLOWAPI.Application.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace GLOWAPI.Tests.Unit.Application;

public class EnderecoGeocodificacaoServiceTests
{
    private readonly Mock<IGeocodificadorService> _geocodificadorService = new();

    [Fact]
    public async Task TentarGeocodificarAsync_DevePersistirCoordenadas_QuandoGeocoderRetornarResultado()
    {
        _geocodificadorService
            .Setup(g => g.GeocodificarEnderecoAsync(It.IsAny<EnderecoGeocodificacaoInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CoordenadaGeografica(-22.9056m, -47.0608m));

        var endereco = OperacaoPerfilValidation.CriarEndereco(
            EnderecoOperacaoDtoBuilder.Criar(),
            mensagem => new InvalidOperationException(mensagem));

        var service = new EnderecoGeocodificacaoService(
            _geocodificadorService.Object,
            NullLogger<EnderecoGeocodificacaoService>.Instance);

        await service.TentarGeocodificarAsync(endereco);

        Assert.Equal(-22.9056m, endereco.Latitude);
        Assert.Equal(-47.0608m, endereco.Longitude);
        Assert.NotNull(endereco.GeocodificadoEm);
    }

    [Fact]
    public async Task TentarGeocodificarAsync_DeveLimparCoordenadas_QuandoGeocoderFalhar()
    {
        _geocodificadorService
            .Setup(g => g.GeocodificarEnderecoAsync(It.IsAny<EnderecoGeocodificacaoInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CoordenadaGeografica?)null);

        var endereco = OperacaoPerfilValidation.CriarEndereco(
            EnderecoOperacaoDtoBuilder.Criar(),
            mensagem => new InvalidOperationException(mensagem));
        endereco.Latitude = -22m;
        endereco.Longitude = -47m;
        endereco.GeocodificadoEm = DateTime.UtcNow;

        var service = new EnderecoGeocodificacaoService(
            _geocodificadorService.Object,
            NullLogger<EnderecoGeocodificacaoService>.Instance);

        await service.TentarGeocodificarAsync(endereco);

        Assert.Null(endereco.Latitude);
        Assert.Null(endereco.Longitude);
        Assert.Null(endereco.GeocodificadoEm);
    }
}
