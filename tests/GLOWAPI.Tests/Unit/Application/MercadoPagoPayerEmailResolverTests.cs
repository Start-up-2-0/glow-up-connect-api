using GLOWAPI.Application.Options;
using GLOWAPI.Application.Services;

namespace GLOWAPI.Tests.Unit.Application;

public class MercadoPagoPayerEmailResolverTests
{
    [Theory]
    [InlineData("TESTUSER978765836", "test_user_978765836@testuser.com")]
    [InlineData("test_user_978765836", "test_user_978765836@testuser.com")]
    [InlineData("test_user_978765836@testuser.com", "test_user_978765836@testuser.com")]
    public void NormalizarEmailTeste_DeveConverterIdentificadorDeContaDeTeste(string entrada, string esperado)
    {
        Assert.Equal(esperado, MercadoPagoPayerEmailResolver.NormalizarEmailTeste(entrada));
    }

    [Fact]
    public void Resolver_DeveUsarOverride_QuandoConfigurado()
    {
        var options = new MercadoPagoOptions
        {
            PayerEmailOverride = "TESTUSER978765836"
        };

        var email = MercadoPagoPayerEmailResolver.Resolver("usuario.real@glow.com", options);

        Assert.Equal("test_user_978765836@testuser.com", email);
    }

    [Fact]
    public void Resolver_DeveUsarEmailDoUsuario_QuandoOverrideNaoConfigurado()
    {
        var options = new MercadoPagoOptions();

        var email = MercadoPagoPayerEmailResolver.Resolver("usuario.real@glow.com", options);

        Assert.Equal("usuario.real@glow.com", email);
    }
}
