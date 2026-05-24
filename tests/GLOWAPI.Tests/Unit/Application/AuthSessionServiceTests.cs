using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Models.Auth;
using GLOWAPI.Application.Options;
using GLOWAPI.Application.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Auth;
using GLOWAPI.Tests.Helpers;
using Microsoft.Extensions.Options;
using Moq;

namespace GLOWAPI.Tests.Unit.Application;

public class AuthSessionServiceTests
{
    private readonly Mock<ISessaoAutenticacaoRepository> _repository = new();
    private readonly Mock<IGlowTokenService> _tokenService = new();
    private readonly AuthOptions _authOptions = new() { RefreshTokenDays = 7, SessionMinutes = 15, MaxLoginAttempts = 5 };

    private AuthSessionService CreateService() => new(
        _repository.Object,
        _tokenService.Object,
        Options.Create(_authOptions));

    [Fact]
    public async Task ObterSessaoAtivaPorRefreshTokenAsync_DeveLancarInvalidToken_QuandoNaoEncontrada()
    {
        _tokenService.Setup(t => t.HashToken("token")).Returns("hash");
        _repository.Setup(r => r.ObterAtivaPorRefreshTokenHashAsync("hash", It.IsAny<CancellationToken>()))
            .ReturnsAsync((SessaoAutenticacao?)null);

        await Assert.ThrowsAsync<InvalidTokenException>(() =>
            CreateService().ObterSessaoAtivaPorRefreshTokenAsync("token"));
    }

    [Fact]
    public async Task ObterSessaoAtivaPorRefreshTokenAsync_DeveLancarTokenExpired_QuandoExpirada()
    {
        var sessao = new SessaoAutenticacao
        {
            ExpiraEm = DateTime.UtcNow.AddMinutes(-1)
        };

        _tokenService.Setup(t => t.HashToken("token")).Returns("hash");
        _repository.Setup(r => r.ObterAtivaPorRefreshTokenHashAsync("hash", It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessao);

        await Assert.ThrowsAsync<TokenExpiredException>(() =>
            CreateService().ObterSessaoAtivaPorRefreshTokenAsync("token"));
    }

    [Fact]
    public async Task ObterSessaoAtivaPorAccessTokenAsync_DeveLancarTokenExpired_QuandoExpirado()
    {
        var metadata = new GlowTokenMetadata(1, 1, DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeSeconds(), UserRole.Cliente);
        _tokenService.Setup(t => t.ValidarMetadata("access")).Returns(metadata);
        _tokenService.Setup(t => t.EstaExpirado(metadata, It.IsAny<DateTime>())).Returns(true);

        await Assert.ThrowsAsync<TokenExpiredException>(() =>
            CreateService().ObterSessaoAtivaPorAccessTokenAsync("access", new AuthSessionContext(null, null)));
    }

    [Fact]
    public async Task RevogarSessaoAsync_DeveMarcarRevogadoEm()
    {
        var sessao = new SessaoAutenticacao { Id = 1 };

        await CreateService().RevogarSessaoAsync(sessao);

        Assert.NotNull(sessao.RevogadoEm);
        _repository.Verify(r => r.Atualizar(sessao), Times.Once);
        _repository.Verify(r => r.SalvarAlteracoesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CriarSessaoComTokensAsync_DevePersistirSessaoComHash()
    {
        var usuario = UsuarioBuilder.Criar();
        var agora = DateTime.UtcNow;

        _tokenService.Setup(t => t.GerarRefreshToken()).Returns("refresh");
        _tokenService.Setup(t => t.HashToken("refresh")).Returns("refresh-hash");
        _tokenService.Setup(t => t.HashToken("access")).Returns("access-hash");
        _tokenService.Setup(t => t.EmitirAccessToken(usuario, It.IsAny<int>(), It.IsAny<DateTime>())).Returns("access");
        _tokenService.Setup(t => t.ObterExpiracaoRefreshToken(It.IsAny<DateTime>())).Returns(agora.AddDays(7));
        _tokenService.Setup(t => t.ObterExpiracaoAccessToken(It.IsAny<DateTime>())).Returns(agora.AddMinutes(15));

        _repository.Setup(r => r.AdicionarAsync(It.IsAny<SessaoAutenticacao>(), It.IsAny<CancellationToken>()))
            .Callback<SessaoAutenticacao, CancellationToken>((s, _) => s.Id = 42)
            .Returns(Task.CompletedTask);

        var result = await CreateService().CriarSessaoComTokensAsync(
            usuario, new AuthSessionContext("127.0.0.1", "agent"));

        Assert.Equal("access", result.AccessToken);
        Assert.Equal("refresh", result.RefreshToken);
        _repository.Verify(r => r.AdicionarAsync(It.IsAny<SessaoAutenticacao>(), It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(r => r.SalvarAlteracoesAsync(It.IsAny<CancellationToken>()), Times.AtLeast(2));
    }
}
