using GLOWAPI.Application.Helpers;

namespace GLOWAPI.Tests.Unit.Application;

public class NomePublicoHelperTests
{
    [Theory]
    [InlineData(null, "Cliente")]
    [InlineData("", "Cliente")]
    [InlineData("   ", "Cliente")]
    [InlineData("maria", "Maria")]
    [InlineData("Maria Silva", "Maria S.")]
    [InlineData("joão da silva santos", "João S.")]
    public void MascararNomeCliente_DeveReduzirExposicao(string? entrada, string esperado)
    {
        Assert.Equal(esperado, NomePublicoHelper.MascararNomeCliente(entrada));
    }
}
