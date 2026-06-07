using GLOWAPI.Application.DTOs.Auth;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Models.Auth;
using GLOWAPI.Application.Options;
using GLOWAPI.Application.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Exceptions.Auth;
using GLOWAPI.Tests.Helpers;
using Microsoft.Extensions.Options;
using Moq;

namespace GLOWAPI.Tests.Unit.Application;

public class AuthServiceTests
{
    private readonly Mock<IUsuarioRepository> _usuarioRepository = new();
    private readonly Mock<IAuthSessionService> _authSessionService = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<ISecurityAuditLogger> _auditLogger = new();
    private readonly AuthOptions _authOptions = new() { MaxLoginAttempts = 5, LockoutMinutes = 15 };
    private readonly AuthSessionContext _context = new("127.0.0.1", "test-agent");

    private AuthService CreateService() => new(
    _usuarioRepository.Object,
    _authSessionService.Object,
    _passwordHasher.Object,
    _auditLogger.Object,
    Mock.Of<IRecuperacaoSenhaRepository>(),
    Mock.Of<IMensagemNotificacaoService>(),
    Options.Create(_authOptions));

    [Fact]
    public async Task LoginAsync_DeveRetornarTokens_QuandoCredenciaisValidas()
    {
        var usuario = UsuarioBuilder.Criar();
        var expiresAccess = DateTime.UtcNow.AddMinutes(15);
        var expiresRefresh = DateTime.UtcNow.AddDays(7);

        _usuarioRepository.Setup(r => r.ObterPorEmailAsync(usuario.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);
        _passwordHasher.Setup(p => p.Verify("Senha123", usuario.Senha)).Returns(true);
        _authSessionService.Setup(s => s.CriarSessaoComTokensAsync(usuario, _context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IssuedTokenPair("access-token", "refresh-token", expiresAccess, expiresRefresh, 10));

        var result = await CreateService().LoginAsync(
            new LoginRequestDto { Email = usuario.Email, Senha = "Senha123" },
            _context);

        Assert.Equal("access-token", result.Token);
        Assert.Equal("refresh-token", result.RefreshToken);
        Assert.Equal(0, usuario.Tentativas);
        _usuarioRepository.Verify(r => r.SalvarAlteracoesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _auditLogger.Verify(a => a.LoginSucceededAsync(usuario.Id, usuario.Email, _context.Ip, _context.UserAgent, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_DeveLancarInvalidCredentials_QuandoUsuarioNaoExiste()
    {
        _usuarioRepository.Setup(r => r.ObterPorEmailAsync("nao@existe.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Usuario?)null);

        await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
            CreateService().LoginAsync(
                new LoginRequestDto { Email = "nao@existe.com", Senha = "Senha123" },
                _context));
    }

    [Fact]
    public async Task LoginAsync_DeveRetornarTokensComFlag_QuandoPendenteConfirmacao()
    {
        var usuario = UsuarioBuilder.Criar(ativo: false);
        usuario.ConfirmacaoTokenHash = "hash";
        usuario.ConfirmacaoExpiraEm = DateTime.UtcNow.AddHours(1);
        var expiresAccess = DateTime.UtcNow.AddMinutes(15);
        var expiresRefresh = DateTime.UtcNow.AddDays(7);

        _usuarioRepository.Setup(r => r.ObterPorEmailAsync(usuario.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);
        _passwordHasher.Setup(p => p.Verify("Senha123", usuario.Senha)).Returns(true);
        _authSessionService.Setup(s => s.CriarSessaoComTokensAsync(usuario, _context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IssuedTokenPair("access-token", "refresh-token", expiresAccess, expiresRefresh, 10));

        var result = await CreateService().LoginAsync(
            new LoginRequestDto { Email = usuario.Email, Senha = "Senha123" },
            _context);

        Assert.Equal("access-token", result.Token);
        Assert.True(result.RequerConfirmacaoEmail);
    }

    [Fact]
    public async Task LoginAsync_DeveLancarInactiveUser_QuandoUsuarioDesativado()
    {
        var usuario = UsuarioBuilder.Criar(ativo: false);
        _usuarioRepository.Setup(r => r.ObterPorEmailAsync(usuario.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);
        _passwordHasher.Setup(p => p.Verify("Senha123", usuario.Senha)).Returns(true);

        await Assert.ThrowsAsync<InactiveUserException>(() =>
            CreateService().LoginAsync(
                new LoginRequestDto { Email = usuario.Email, Senha = "Senha123" },
                _context));
    }

    [Fact]
    public async Task LoginAsync_DeveLancarUserBlocked_QuandoTentativasExcedidas()
    {
        var usuario = UsuarioBuilder.Criar(tentativas: 5);
        _usuarioRepository.Setup(r => r.ObterPorEmailAsync(usuario.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);

        await Assert.ThrowsAsync<UserBlockedException>(() =>
            CreateService().LoginAsync(
                new LoginRequestDto { Email = usuario.Email, Senha = "Senha123" },
                _context));
    }

    [Fact]
    public async Task LoginAsync_DeveAplicarBloqueioTemporario_QuandoExcedeTentativas()
    {
        var usuario = UsuarioBuilder.Criar(tentativas: 4);
        _usuarioRepository.Setup(r => r.ObterPorEmailAsync(usuario.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);
        _passwordHasher.Setup(p => p.Verify("errada", usuario.Senha)).Returns(false);

        await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
            CreateService().LoginAsync(
                new LoginRequestDto { Email = usuario.Email, Senha = "errada" },
                _context));

        Assert.Equal(5, usuario.Tentativas);
        Assert.NotNull(usuario.BloqueadoAte);
    }

    [Fact]
    public async Task LoginAsync_DeveIncrementarTentativas_QuandoSenhaInvalida()
    {
        var usuario = UsuarioBuilder.Criar(tentativas: 2);
        _usuarioRepository.Setup(r => r.ObterPorEmailAsync(usuario.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);
        _passwordHasher.Setup(p => p.Verify("errada", usuario.Senha)).Returns(false);

        await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
            CreateService().LoginAsync(
                new LoginRequestDto { Email = usuario.Email, Senha = "errada" },
                _context));

        Assert.Equal(3, usuario.Tentativas);
        _usuarioRepository.Verify(r => r.SalvarAlteracoesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LogoutAsync_DeveRevogarSessao_QuandoValida()
    {
        var sessao = new SessaoAutenticacao { Id = 1, UsuarioId = 1 };
        var auth = new AuthenticatedSessionResult(
            new SessaoAutenticacaoInfo(1, 1, null, null),
            new UsuarioAuthInfo(1, "Teste", "test@email.com", GLOWAPI.Domain.Enums.UserRole.Cliente));

        _authSessionService.Setup(s => s.ObterSessaoAtivaPorAccessTokenAsync("token", It.IsAny<AuthSessionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(auth);
        _authSessionService.Setup(s => s.ObterSessaoPorIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessao);

        await CreateService().LogoutAsync("token");

        _authSessionService.Verify(s => s.RevogarSessaoAsync(sessao, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RefreshAsync_DeveRotacionarTokens_QuandoRefreshValido()
    {
        var usuario = UsuarioBuilder.Criar();
        var sessao = new SessaoAutenticacao { Id = 1, UsuarioId = usuario.Id, ExpiraEm = DateTime.UtcNow.AddDays(1) };
        var expiresAccess = DateTime.UtcNow.AddMinutes(15);
        var expiresRefresh = DateTime.UtcNow.AddDays(7);

        _authSessionService.Setup(s => s.ObterSessaoAtivaPorRefreshTokenAsync("refresh", It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessao);
        _usuarioRepository.Setup(r => r.ObterPorIdAsync(usuario.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);
        _authSessionService.Setup(s => s.RotacionarSessaoAsync(sessao, usuario, _context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IssuedTokenPair("new-access", "new-refresh", expiresAccess, expiresRefresh, 2));

        var result = await CreateService().RefreshAsync(
            new RefreshTokenRequestDto { RefreshToken = "refresh" },
            _context);

        Assert.Equal("new-access", result.Token);
        Assert.Equal("new-refresh", result.RefreshToken);
    }

    [Fact]
    public async Task RefreshAsync_DeveLancarInvalidToken_QuandoSessaoInvalida()
    {
        _authSessionService.Setup(s => s.ObterSessaoAtivaPorRefreshTokenAsync("bad", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidTokenException());

        await Assert.ThrowsAsync<InvalidTokenException>(() =>
            CreateService().RefreshAsync(
                new RefreshTokenRequestDto { RefreshToken = "bad" },
                _context));
    }
}
