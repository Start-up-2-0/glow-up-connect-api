using GLOWAPI.Application.Services;

namespace GLOWAPI.Tests.Unit.Application;

public class AssinaturaValorCobrancaTests
{
    [Theory]
    [InlineData(199.90, 50, 99.95)]
    [InlineData(100, 50, 50)]
    [InlineData(199.90, 0, 199.90)]
    public void CalcularMensalidade_DeveAplicarDescontoPermanente(
        decimal precoPlano,
        decimal percentualDesconto,
        decimal esperado)
    {
        var valor = AssinaturaValorCobranca.CalcularMensalidade(precoPlano, percentualDesconto);
        Assert.Equal(esperado, valor);
    }

    [Fact]
    public void CalcularMensalidade_SemDescontoInformado_DeveRetornarPrecoPlano()
    {
        var valor = AssinaturaValorCobranca.CalcularMensalidade(199.90m, null);
        Assert.Equal(199.90m, valor);
    }
}
