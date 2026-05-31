using GLOWAPI.Application.Helpers;

namespace GLOWAPI.Tests.Unit.Application;

public class GeolocalizacaoHelperTests
{
    [Fact]
    public void CalcularDistanciaKm_DeveRetornarDistanciaBaixa_ParaPontosProximos()
    {
        var distancia = GeolocalizacaoHelper.CalcularDistanciaKm(
            -22.9056m,
            -47.0608m,
            -22.9060m,
            -47.0610m);

        Assert.True(distancia < 1);
    }

    [Fact]
    public void NormalizarTextoLocalizacao_DeveRemoverAcentos()
    {
        var normalizado = GeolocalizacaoHelper.NormalizarTextoLocalizacao("São Paulo");

        Assert.Equal("sao paulo", normalizado);
    }
}
