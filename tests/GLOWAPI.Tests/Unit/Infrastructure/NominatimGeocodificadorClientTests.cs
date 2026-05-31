using System.Net;
using System.Text.Json;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Models.Geolocalizacao;
using GLOWAPI.Application.Options;
using GLOWAPI.Infrastructure.Geolocalizacao;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;

namespace GLOWAPI.Tests.Unit.Infrastructure;

public class NominatimGeocodificadorClientTests
{
    [Fact]
    public async Task GeocodificarEnderecoAsync_DeveRetornarCoordenadas_QuandoApiResponderComResultado()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""[{"lat":"-22.9056","lon":"-47.0608"}]""")
            });

        var client = new HttpClient(handler.Object)
        {
            BaseAddress = new Uri("https://nominatim.test/")
        };

        var service = new NominatimGeocodificadorClient(
            client,
            Options.Create(new GeocodificacaoOptions()),
            NullLogger<NominatimGeocodificadorClient>.Instance);

        var resultado = await service.GeocodificarEnderecoAsync("Rua A, Campinas, SP, Brasil");

        Assert.NotNull(resultado);
        Assert.Equal(-22.9056m, resultado!.Latitude);
        Assert.Equal(-47.0608m, resultado.Longitude);
    }

    [Fact]
    public async Task ReverseGeocodificarAsync_DeveRetornarCidadeEEstado_QuandoApiResponderComEndereco()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                    {
                      "address": {
                        "city": "Campinas",
                        "state": "Sao Paulo",
                        "ISO3166-2-lvl4": "BR-SP"
                      }
                    }
                    """)
            });

        var client = new HttpClient(handler.Object)
        {
            BaseAddress = new Uri("https://nominatim.test/")
        };

        var service = new NominatimGeocodificadorClient(
            client,
            Options.Create(new GeocodificacaoOptions()),
            NullLogger<NominatimGeocodificadorClient>.Instance);

        var resultado = await service.ReverseGeocodificarAsync(-22.9056m, -47.0608m);

        Assert.NotNull(resultado);
        Assert.Equal("Campinas", resultado!.Cidade);
        Assert.Equal("SP", resultado.Estado);
    }
}
