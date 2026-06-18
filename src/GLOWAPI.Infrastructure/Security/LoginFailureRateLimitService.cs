using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace GLOWAPI.Infrastructure.Security;

public class LoginFailureRateLimitService : ILoginFailureRateLimitService
{
    private readonly IMemoryCache _cache;
    private readonly RateLimitOptions _options;

    public LoginFailureRateLimitService(IMemoryCache cache, IOptions<RateLimitOptions> options)
    {
        _cache = cache;
        _options = options.Value;
    }

    public Task<bool> PodeTentarAsync(string ip, CancellationToken cancellationToken = default)
    {
        var count = ObterContagem(ip);
        return Task.FromResult(count < _options.SensitiveMaxRequests);
    }

    public Task RegistrarFalhaAsync(string ip, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"login-fail:{ip}";
        var janela = TimeSpan.FromSeconds(Math.Max(1, _options.SensitiveWindowSeconds));

        var count = _cache.GetOrCreate(cacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = janela;
            return 0;
        });

        _cache.Set(cacheKey, count + 1, janela);
        return Task.CompletedTask;
    }

    private int ObterContagem(string ip)
    {
        return _cache.TryGetValue($"login-fail:{ip}", out int count) ? count : 0;
    }
}
