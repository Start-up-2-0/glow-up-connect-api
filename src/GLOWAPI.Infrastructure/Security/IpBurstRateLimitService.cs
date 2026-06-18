using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Options;
using GLOWAPI.Domain.Entities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace GLOWAPI.Infrastructure.Security;

public class IpBurstRateLimitService : IIpBurstRateLimitService
{
    private readonly IIpRateLimitBlockRepository _blockRepository;
    private readonly IMemoryCache _cache;
    private readonly RateLimitOptions _options;

    public IpBurstRateLimitService(
        IIpRateLimitBlockRepository blockRepository,
        IMemoryCache cache,
        IOptions<RateLimitOptions> options)
    {
        _blockRepository = blockRepository;
        _cache = cache;
        _options = options.Value;
    }

    public async Task<IpBurstRateLimitResult> AvaliarAsync(string ip, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return new IpBurstRateLimitResult(true, false, null);
        }

        var bloqueioAtivo = await _blockRepository.ObterBloqueioAtivoAsync(ip, cancellationToken);
        if (bloqueioAtivo is not null)
        {
            return new IpBurstRateLimitResult(false, false, bloqueioAtivo.BlockedUntil);
        }

        var cacheKey = $"rate-limit:burst:{ip}";
        var agora = DateTime.UtcNow;
        var janela = TimeSpan.FromSeconds(Math.Max(1, _options.BurstWindowSeconds));

        var timestamps = _cache.GetOrCreate(cacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = janela;
            return new List<DateTime>();
        })!;

        lock (timestamps)
        {
            timestamps.RemoveAll(t => agora - t > janela);
            timestamps.Add(agora);

            if (timestamps.Count <= _options.BurstMaxRequests)
            {
                return new IpBurstRateLimitResult(true, false, null);
            }
        }

        var blockedUntil = agora.AddHours(Math.Max(1, _options.BlockDurationHours));
        await _blockRepository.RegistrarBloqueioAsync(new IpRateLimitBlock
        {
            Ip = ip,
            BlockedUntil = blockedUntil,
            Reason = "IP_BURST_BLOCKED"
        }, cancellationToken);

        _cache.Remove(cacheKey);

        return new IpBurstRateLimitResult(false, true, blockedUntil);
    }
}
