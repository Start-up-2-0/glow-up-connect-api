using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Options;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Infrastructure.Security;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;

namespace GLOWAPI.Tests.Unit.Infrastructure;

public class IpBurstRateLimitServiceTests
{
    private readonly Mock<IIpRateLimitBlockRepository> _repository = new();
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());

    [Fact]
    public async Task AvaliarAsync_DeveIgnorar_QuandoTrafegoConfiavel()
    {
        var service = CreateService();

        var resultado = await service.AvaliarAsync("10.0.0.1", "/api/planos", trafegoConfiavel: true);

        Assert.True(resultado.Permitido);
        _repository.Verify(
            r => r.ObterBloqueioAtivoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task AvaliarAsync_DeveRetornarSoftBurst_QuandoExcederLimiteGeral()
    {
        var service = CreateService(new RateLimitOptions
        {
            Enabled = true,
            BurstMaxRequests = 2,
            BurstWindowSeconds = 60,
            BurstPenaltySeconds = 15,
            HardBlockMultiplier = 5,
            ExemptAuthenticatedRequests = true
        });

        Assert.True((await service.AvaliarAsync("10.0.0.2", "/api/planos", false)).Permitido);
        Assert.True((await service.AvaliarAsync("10.0.0.2", "/api/planos", false)).Permitido);

        var resultado = await service.AvaliarAsync("10.0.0.2", "/api/planos", false);

        Assert.False(resultado.Permitido);
        Assert.Equal(IpBurstRateLimitService.ErrorCodeSoftBurst, resultado.ErrorCode);
        Assert.Equal(15, resultado.RetryAfterSeconds);
        _repository.Verify(
            r => r.RegistrarBloqueioAsync(It.IsAny<IpRateLimitBlock>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task AvaliarAsync_DeveBloquear24h_QuandoAbusoExtremo()
    {
        var service = CreateService(new RateLimitOptions
        {
            Enabled = true,
            BurstMaxRequests = 2,
            BurstWindowSeconds = 60,
            HardBlockMultiplier = 3,
            BlockDurationHours = 24,
            ExemptAuthenticatedRequests = true
        });

        for (var i = 0; i < 6; i++)
        {
            await service.AvaliarAsync("10.0.0.3", "/api/planos", false);
        }

        var resultado = await service.AvaliarAsync("10.0.0.3", "/api/planos", false);

        Assert.False(resultado.Permitido);
        Assert.Equal(IpBurstRateLimitService.ErrorCodeHardBlock, resultado.ErrorCode);
        _repository.Verify(
            r => r.RegistrarBloqueioAsync(It.IsAny<IpRateLimitBlock>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private IpBurstRateLimitService CreateService(RateLimitOptions? options = null) =>
        new(
            _repository.Object,
            _cache,
            Options.Create(options ?? new RateLimitOptions
            {
                Enabled = true,
                BurstMaxRequests = 80,
                BurstWindowSeconds = 60,
                ExemptAuthenticatedRequests = true
            }));
}
