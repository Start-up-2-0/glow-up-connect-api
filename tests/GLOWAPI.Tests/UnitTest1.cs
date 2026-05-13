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
        Assert.NotNull(usuario.Senha);
        Assert.NotEqual("Senha123", usuario.Senha);

        repoMock.Verify(r => r.AdicionarAsync(It.IsAny<Usuario>(), It.IsAny<CancellationToken>()), Times.Once);
        repoMock.Verify(r => r.SalvarAlteracoesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
    [Fact]
    public async Task CriarUsuarioAsync_DeveLancarExcecao_QuandoEmailJaExiste()
    {
        var UsuarioExistente = new Usuario
        {
            Id = 1,
            Nome = "Maria",
            Email = "maria@email.com",
            Senha = "hash123"
        };

        var repoMock = new Mock<IUsuarioRepository>();
        repoMock.Setup(r => r.ObterPorEmailAsync("maria@email.com", It.IsAny<CancellationToken>()))
                .ReturnsAsync(UsuarioExistente);

        var service = new UsuarioService(repoMock.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CriarUsuarioAsync(
                "João", "maria@email.com", "11999999999", "Senha123", UserRole.DonoEstabelecimento));
    }

    [Fact]
    public async Task ObterUsuarioPorIdAsync_DeveRetornarUsuario_QuandoExiste()
    {   
        // Arange
        var UsuarioEsperado = new Usuario
        {
            Id = 1,
            Nome = "João",
            Email = "joao@email.com",
            Ativo = true  
        };

        var repoMock = new Mock<IUsuarioRepository>();
        repoMock.Setup(r => r.ObterPorIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(UsuarioEsperado);

        var service = new UsuarioService(repoMock.Object);

        //Act
        var resultado = await service.ObterUsuarioPorIdAsync(1);

        //Assert
        Assert.NotNull(resultado);
        Assert.Equal(1, resultado.Id);
        Assert.Equal("João", resultado.Nome);
        Assert.Equal("joao@email.com", resultado.Email);
    }

    [Fact]
    public async Task ObterUsuarioPorIdAsync_DeveRetornarNull_QuandoNaoExiste()
    {
        // Arrange
        var repoMock = new Mock<IUsuarioRepository>();
        repoMock.Setup(r => r.ObterPorIdAsync(999, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Usuario?)null);

        var service = new UsuarioService(repoMock.Object);

        // Act
        var resultado = await service.ObterUsuarioPorIdAsync(999);

        // Assert
        Assert.Null(resultado);
    }

    [Fact]
    public async Task AtualizarUsuarioAsync_DeveAtualizar_QuandoUsuarioExisteEAtivo()
    {
        // Arrange
        var usuarioExistente = new Usuario
        {
            Id = 1,
            Nome = "João",
            Email = "joao@email.com",
            Telefone = "11999999999",
            Ativo = true
        };

        var repoMock = new Mock<IUsuarioRepository>();
        repoMock.Setup(r => r.ObterPorIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(usuarioExistente);

        var service = new UsuarioService(repoMock.Object);

        // Act
        await service.AtualizarUsuarioAsync(1, "João Silva", "11988888888");

        // Assert
        Assert.Equal("João Silva", usuarioExistente.Nome);
        Assert.Equal("11988888888", usuarioExistente.Telefone);
        Assert.NotNull(usuarioExistente.UpdatedAt);

        repoMock.Verify(r => r.Atualizar(usuarioExistente), Times.Once);
        repoMock.Verify(r => r.SalvarAlteracoesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AtualizarUsuarioAsync_DeveLancarExcecao_QuandoUsuarioNaoExiste()
    {
        // Arrange
        var repoMock = new Mock<IUsuarioRepository>();
        repoMock.Setup(r => r.ObterPorIdAsync(999, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Usuario?)null);

        var service = new UsuarioService(repoMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.AtualizarUsuarioAsync(999, "João", "11988888888"));
    }

    [Fact]
    public async Task AtualizarUsuarioAsync_DeveLancarExcecao_QuandoUsuarioInativo()
    {
        // Arrange
        var usuarioInativo = new Usuario
        {
            Id = 1,
            Nome = "João",
            Ativo = false
        };

        var repoMock = new Mock<IUsuarioRepository>();
        repoMock.Setup(r => r.ObterPorIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(usuarioInativo);

        var service = new UsuarioService(repoMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.AtualizarUsuarioAsync(1, "João Silva", "11988888888"));
    }

    [Fact]
    public async Task DesativarUsuarioAsync_DeveDesativar_QuandoUsuarioExiste()
    {
        // Arrange
        var usuarioAtivo = new Usuario
        {
            Id = 1,
            Nome = "João",
            Ativo = true
        };

        var repoMock = new Mock<IUsuarioRepository>();
        repoMock.Setup(r => r.ObterPorIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(usuarioAtivo);

        var service = new UsuarioService(repoMock.Object);

        // Act
        await service.DesativarUsuarioAsync(1);

        // Assert
        Assert.False(usuarioAtivo.Ativo);
        Assert.NotNull(usuarioAtivo.UpdatedAt);

        repoMock.Verify(r => r.Atualizar(usuarioAtivo), Times.Once);
        repoMock.Verify(r => r.SalvarAlteracoesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DesativarUsuarioAsync_DeveLancarExcecao_QuandoUsuarioNaoExiste()
    {
        // Arrange
        var repoMock = new Mock<IUsuarioRepository>();
        repoMock.Setup(r => r.ObterPorIdAsync(999, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Usuario?)null);

        var service = new UsuarioService(repoMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.DesativarUsuarioAsync(999));
    }

}