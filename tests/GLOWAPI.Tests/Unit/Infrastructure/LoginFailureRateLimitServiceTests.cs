using System.Net;
using GLOWAPI.Application.Options;
using GLOWAPI.Infrastructure.Security;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace GLOWAPI.Tests.Unit.Infrastructure;

public class LoginFailureRateLimitServiceTests
{
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());

    [Fact]
    public async Task RegistrarFalhaAsync_DeveBloquearAposLimite()
    {
        var service = CreateService(new RateLimitOptions
        {
            SensitiveMaxRequests = 2,
            SensitiveWindowSeconds = 60
        });

        Assert.True(await service.PodeTentarAsync("10.0.0.5"));
        await service.RegistrarFalhaAsync("10.0.0.5");
        Assert.True(await service.PodeTentarAsync("10.0.0.5"));
        await service.RegistrarFalhaAsync("10.0.0.5");
        Assert.False(await service.PodeTentarAsync("10.0.0.5"));
    }

    private LoginFailureRateLimitService CreateService(RateLimitOptions options) =>
        new(_cache, Options.Create(options));
}
