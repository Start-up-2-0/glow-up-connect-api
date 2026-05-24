using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Tests.Unit.Domain;

public class UsuarioAuthTests
{
    [Fact]
    public void RegistrarTentativaFalha_IncrementaTentativas()
    {
        var usuario = new Usuario { Tentativas = 2 };

        usuario.RegistrarTentativaFalha();

        Assert.Equal(3, usuario.Tentativas);
        Assert.NotNull(usuario.UpdatedAt);
    }

    [Fact]
    public void ResetarTentativas_ZeraContador()
    {
        var usuario = new Usuario { Tentativas = 4 };

        usuario.ResetarTentativas();

        Assert.Equal(0, usuario.Tentativas);
        Assert.NotNull(usuario.UpdatedAt);
    }

    [Fact]
    public void EstaBloqueado_QuandoAtingeLimite_RetornaTrue()
    {
        var usuario = new Usuario { Tentativas = 5 };

        Assert.True(usuario.EstaBloqueado(5));
    }

    [Fact]
    public void PodeAutenticar_QuandoInativo_RetornaFalse()
    {
        var usuario = new Usuario { Ativo = false, Tentativas = 0 };

        Assert.False(usuario.PodeAutenticar(5));
    }

    [Fact]
    public void PodeAutenticar_QuandoBloqueado_RetornaFalse()
    {
        var usuario = new Usuario { Ativo = true, Tentativas = 5 };

        Assert.False(usuario.PodeAutenticar(5));
    }

    [Fact]
    public void PodeAutenticar_QuandoAtivoENaoBloqueado_RetornaTrue()
    {
        var usuario = new Usuario { Ativo = true, Tentativas = 2 };

        Assert.True(usuario.PodeAutenticar(5));
    }
}
