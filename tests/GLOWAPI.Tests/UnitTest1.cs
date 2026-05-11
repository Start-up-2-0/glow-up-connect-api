using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using Moq;

namespace GLOWAPI.Tests;

public class UsuarioServiceTests
{
    [Fact]
    public async Task CriarUsuarioAsync_DeveCriarUsuario_QuandoEmailNaoExiste()
    {
        // Arrange
        var repoMock = new Mock<IUsuarioRepository>();
        repoMock.Setup(r => r.ObterPorEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Usuario?)null);

        var service = new UsuarioService(repoMock.Object);

        // Act
        var usuario = await service.CriarUsuarioAsync(
            "João",
            "joao@email.com",
            "11999999999",
            "Senha123",
            UserRole.DonoEstabelecimento);

        // Assert
        Assert.NotNull(usuario);
        Assert.Equal("João", usuario.Nome);
        Assert.Equal("joao@email.com", usuario.Email);
        Assert.True(usuario.Ativo);
        Assert.NotNull(usuario.SenhaHash);
        Assert.NotEqual("Senha123", usuario.SenhaHash);

        repoMock.Verify(r => r.AdicionarAsync(It.IsAny<Usuario>(), It.IsAny<CancellationToken>()), Times.Once);
        repoMock.Verify(r => r.SalvarAlteracoesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}