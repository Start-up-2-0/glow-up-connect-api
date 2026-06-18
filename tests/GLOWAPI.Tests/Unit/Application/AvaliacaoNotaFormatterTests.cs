using GLOWAPI.Application.Helpers;
using Xunit;

namespace GLOWAPI.Tests.Unit.Application;

public class AvaliacaoNotaFormatterTests
{
    [Theory]
    [InlineData(4.75, 4.7)]
    [InlineData(4.76, 4.8)]
    [InlineData(5.0, 5.0)]
    [InlineData(0.0, 0.0)]
    [InlineData(4.71, 4.7)]
    [InlineData(4.79, 4.8)]
    public void FormatarMediaIfood_DeveAplicarRegraDeArredondamento(double entrada, double esperado)
    {
        var resultado = AvaliacaoNotaFormatter.FormatarMediaIfood((decimal)entrada);
        Assert.Equal((decimal)esperado, resultado);
    }

    [Fact]
    public void CalcularMediaBruta_DeveRetornarMediaSimples()
    {
        var notas = new byte[] { 5, 4, 3 };
        var media = AvaliacaoNotaFormatter.CalcularMediaBruta(notas);
        Assert.Equal(4m, media);
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(5, true)]
    [InlineData(6, false)]
    public void NotaValida_DeveAceitarSomenteZeroACinco(int nota, bool valida)
    {
        Assert.Equal(valida, AvaliacaoNotaFormatter.NotaValida(nota));
    }
}
