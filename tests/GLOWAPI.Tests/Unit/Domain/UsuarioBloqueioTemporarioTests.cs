using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Tests.Unit.Domain;

public class UsuarioBloqueioTemporarioTests
{
    [Fact]
    public void EstaBloqueado_DeveRetornarTrue_QuandoBloqueadoAteNoFuturo()
    {
        var usuario = new Usuario { BloqueadoAte = DateTime.UtcNow.AddMinutes(10), Tentativas = 0 };

        Assert.True(usuario.EstaBloqueado(5));
    }

    [Fact]
    public void EstaBloqueado_DeveRetornarFalse_QuandoBloqueadoAteExpirou()
    {
        var usuario = new Usuario { BloqueadoAte = DateTime.UtcNow.AddMinutes(-1), Tentativas = 0 };

        Assert.False(usuario.EstaBloqueado(5));
    }

    [Fact]
    public void EstaBloqueado_DeveRetornarTrue_QuandoTentativasAtingemLimite()
    {
        var usuario = new Usuario { Tentativas = 5 };

        Assert.True(usuario.EstaBloqueado(5));
    }

    [Fact]
    public void AplicarBloqueioTemporario_DeveDefinirBloqueadoAte()
    {
        var usuario = new Usuario();

        usuario.AplicarBloqueioTemporario(15);

        Assert.NotNull(usuario.BloqueadoAte);
        Assert.True(usuario.BloqueadoAte > DateTime.UtcNow);
    }

    [Fact]
    public void ResetarTentativas_DeveLimparBloqueadoAte()
    {
        var usuario = new Usuario { Tentativas = 5, BloqueadoAte = DateTime.UtcNow.AddMinutes(10) };

        usuario.ResetarTentativas();

        Assert.Equal(0, usuario.Tentativas);
        Assert.Null(usuario.BloqueadoAte);
    }
}
