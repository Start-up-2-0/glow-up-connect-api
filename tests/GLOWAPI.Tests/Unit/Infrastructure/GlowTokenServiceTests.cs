using GLOWAPI.Application.Options;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Infrastructure.Security;
using Microsoft.Extensions.Options;

namespace GLOWAPI.Tests.Unit.Infrastructure;

public class GlowTokenServiceTests
{
    private readonly GlowTokenService _service;

    public GlowTokenServiceTests()
    {
        var options = Options.Create(new AuthOptions
        {
            TokenSalt = "glow-dev-token-salt-min-32-chars!!",
            SessionMinutes = 15
        });

        _service = new GlowTokenService(options);
    }

    [Fact]
    public void EmitirAccessToken_DeveGerarTokenComDuasPartes()
    {
        var usuario = new Usuario { Id = 1, Email = "test@email.com", Role = UserRole.Cliente };

        var token = _service.EmitirAccessToken(usuario, 10, DateTime.UtcNow);

        var parts = token.Split('.');
        Assert.Equal(2, parts.Length);
        Assert.False(string.IsNullOrWhiteSpace(parts[0]));
        Assert.False(string.IsNullOrWhiteSpace(parts[1]));
    }

    [Fact]
    public void ValidarMetadata_DeveRetornarMetadata_QuandoTokenValido()
    {
        var usuario = new Usuario { Id = 5, Email = "test@email.com", Role = UserRole.Admin };
        var issuedAt = DateTime.UtcNow;

        var token = _service.EmitirAccessToken(usuario, 20, issuedAt);
        var metadata = _service.ValidarMetadata(token);

        Assert.NotNull(metadata);
        Assert.Equal(5, metadata!.UserId);
        Assert.Equal(20, metadata.SessionId);
        Assert.Equal(UserRole.Admin, metadata.Role);
    }

    [Fact]
    public void ValidarMetadata_DeveRetornarNull_QuandoAssinaturaInvalida()
    {
        var usuario = new Usuario { Id = 1, Role = UserRole.Cliente };
        var token = _service.EmitirAccessToken(usuario, 1, DateTime.UtcNow) + "x";

        Assert.Null(_service.ValidarMetadata(token));
    }

    [Fact]
    public void EstaExpirado_DeveRetornarTrue_QuandoPassouSessionMinutes()
    {
        var issuedAt = DateTime.UtcNow.AddMinutes(-20);
        var metadata = new GLOWAPI.Application.Interfaces.Services.GlowTokenMetadata(
            1, 1, new DateTimeOffset(issuedAt).ToUnixTimeSeconds(), UserRole.Cliente);

        Assert.True(_service.EstaExpirado(metadata, DateTime.UtcNow));
    }

    [Fact]
    public void HashToken_DeveSerDeterministico()
    {
        var hash1 = _service.HashToken("same-token");
        var hash2 = _service.HashToken("same-token");

        Assert.Equal(hash1, hash2);
        Assert.NotEqual(_service.HashToken("other-token"), hash1);
    }

    [Fact]
    public void GerarRefreshToken_DeveGerarValoresDistintos()
    {
        var token1 = _service.GerarRefreshToken();
        var token2 = _service.GerarRefreshToken();

        Assert.NotEqual(token1, token2);
        Assert.False(string.IsNullOrWhiteSpace(token1));
    }
}
