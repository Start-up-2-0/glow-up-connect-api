using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using Moq;

namespace GLOWAPI.Tests.Unit.Application;

public class UsuarioServiceTests
{
    private readonly Mock<IPasswordHasher> _passwordHasher = new();

    public UsuarioServiceTests()
    {
        _passwordHasher.Setup(p => p.Hash(It.IsAny<string>())).Returns("hashed-password");
    }

    [Fact]
    public async Task CriarUsuarioAsync_DeveCriarUsuario_QuandoEmailNaoExiste()
    {
        var repoMock = new Mock<IUsuarioRepository>();
        repoMock.Setup(r => r.ObterPorEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Usuario?)null);

        var service = new UsuarioService(repoMock.Object, _passwordHasher.Object);

        var usuario = await service.CriarUsuarioAsync(
            "João",
            "joao@email.com",
            "11999999999",
            "Senha123",
            UserRole.DonoEstabelecimento);

        Assert.NotNull(usuario);
        Assert.Equal("João", usuario.Nome);
        Assert.Equal("joao@email.com", usuario.Email);
        Assert.True(usuario.Ativo);
        Assert.Equal("hashed-password", usuario.Senha);

        repoMock.Verify(r => r.AdicionarAsync(It.IsAny<Usuario>(), It.IsAny<CancellationToken>()), Times.Once);
        repoMock.Verify(r => r.SalvarAlteracoesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CriarUsuarioAsync_DeveLancarExcecao_QuandoEmailJaExiste()
    {
        var usuarioExistente = new Usuario { Id = 1, Email = "maria@email.com", Senha = "hash123" };

        var repoMock = new Mock<IUsuarioRepository>();
        repoMock.Setup(r => r.ObterPorEmailAsync("maria@email.com", It.IsAny<CancellationToken>()))
                .ReturnsAsync(usuarioExistente);

        var service = new UsuarioService(repoMock.Object, _passwordHasher.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CriarUsuarioAsync(
                "João", "maria@email.com", "11999999999", "Senha123", UserRole.DonoEstabelecimento));
    }
}
