using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.DTOs.Usuario;
using GLOWAPI.Application.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Usuario;
using Moq;

namespace GLOWAPI.Tests.Unit.Application;

public class UsuarioServiceTests
{
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<ICurrentUserContext> _currentUser = new();
    private readonly Mock<IAuthSessionService> _authSessionService = new();

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

        var service = CreateService(repoMock.Object);

        var usuario = await service.CriarUsuarioAsync(new CriarUsuarioDto
        {
            Nome = "João",
            Email = "joao@email.com",
            Telefone = "11999999999",
            Senha = "Senha123",
            Role = UserRole.DonoEstabelecimento
        });

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

        var service = CreateService(repoMock.Object);

        await Assert.ThrowsAsync<EmailJaCadastradoException>(() =>
            service.CriarUsuarioAsync(new CriarUsuarioDto
            {
                Nome = "João",
                Email = "maria@email.com",
                Telefone = "11999999999",
                Senha = "Senha123",
                Role = UserRole.DonoEstabelecimento
            }));
    }

    [Fact]
    public async Task ObterPerfilAtualAsync_DeveRetornarUsuario_QuandoAtivo()
    {
        var usuario = new Usuario { Id = 1, Nome = "João", Ativo = true };
        var repoMock = new Mock<IUsuarioRepository>();
        repoMock.Setup(r => r.ObterPorIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);

        _currentUser.Setup(c => c.UserId).Returns(1);

        var service = CreateService(repoMock.Object);
        var result = await service.ObterPerfilAtualAsync();

        Assert.Equal("João", result.Nome);
    }

    [Fact]
    public async Task ObterPerfilAtualAsync_DeveLancarExcecao_QuandoInativo()
    {
        var usuario = new Usuario { Id = 1, Ativo = false };
        var repoMock = new Mock<IUsuarioRepository>();
        repoMock.Setup(r => r.ObterPorIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);

        _currentUser.Setup(c => c.UserId).Returns(1);

        var service = CreateService(repoMock.Object);

        await Assert.ThrowsAsync<UsuarioNaoEncontradoException>(() => service.ObterPerfilAtualAsync());
    }

    [Fact]
    public async Task DesativarContaAtualAsync_DeveRevogarSessao()
    {
        var usuario = new Usuario { Id = 1, Ativo = true };
        var sessao = new GLOWAPI.Domain.Entities.SessaoAutenticacao { Id = 10, UsuarioId = 1 };

        var repoMock = new Mock<IUsuarioRepository>();
        repoMock.Setup(r => r.ObterPorIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);

        _currentUser.Setup(c => c.UserId).Returns(1);
        _currentUser.Setup(c => c.SessionId).Returns(10);

        _authSessionService.Setup(s => s.ObterSessaoPorIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessao);

        var service = CreateService(repoMock.Object);
        await service.DesativarContaAtualAsync();

        Assert.False(usuario.Ativo);
        _authSessionService.Verify(s => s.RevogarSessaoAsync(sessao, It.IsAny<CancellationToken>()), Times.Once);
        repoMock.Verify(r => r.SalvarAlteracoesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private UsuarioService CreateService(IUsuarioRepository repository) =>
        new(repository, _passwordHasher.Object, _currentUser.Object, _authSessionService.Object);
}
