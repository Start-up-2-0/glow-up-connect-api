using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Options;
using GLOWAPI.Domain.Entities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace GLOWAPI.Infrastructure.Security;

public class IpBurstRateLimitService : IIpBurstRateLimitService
{
    public const string ErrorCodeHardBlock = "IP_BLOCKED_24H";
    public const string ErrorCodeSoftBurst = "RATE_LIMIT_BURST";

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

    public async Task<IpBurstRateLimitResult> AvaliarAsync(
        string ip,
        string path,
        bool possuiTokenAutenticacao,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return new IpBurstRateLimitResult(true, false, null);
        }

        if (_options.ExemptAuthenticatedRequests && possuiTokenAutenticacao)
        {
            return new IpBurstRateLimitResult(true, false, null);
        }

        var bloqueioAtivo = await _blockRepository.ObterBloqueioAtivoAsync(ip, cancellationToken);
        if (bloqueioAtivo is not null)
        {
            return new IpBurstRateLimitResult(false, false, bloqueioAtivo.BlockedUntil, ErrorCodeHardBlock);
        }

        var sensivel = EhRotaSensivel(path);
        var maxRequests = sensivel ? _options.SensitiveMaxRequests : _options.BurstMaxRequests;
        var janelaSegundos = sensivel ? _options.SensitiveWindowSeconds : _options.BurstWindowSeconds;
        var penaltySeconds = sensivel ? _options.SensitivePenaltySeconds : _options.BurstPenaltySeconds;
        var hardMultiplier = sensivel ? _options.SensitiveHardBlockMultiplier : _options.HardBlockMultiplier;
        var cachePrefix = sensivel ? "rate-limit:sensitive" : "rate-limit:burst";

        var contagem = RegistrarRequisicao($"{cachePrefix}:{ip}", janelaSegundos);

        if (contagem <= maxRequests)
        {
            return new IpBurstRateLimitResult(true, false, null);
        }

        if (contagem > maxRequests * Math.Max(2, hardMultiplier))
        {
            return await RegistrarBloqueioDuroAsync(ip, sensivel, cancellationToken);
        }

        return new IpBurstRateLimitResult(
            false,
            true,
            null,
            ErrorCodeSoftBurst,
            Math.Max(1, penaltySeconds));
    }

    private bool EhRotaSensivel(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var normalizado = path.TrimEnd('/');
        return _options.SensitivePathPrefixes.Any(prefix =>
            normalizado.StartsWith(prefix.TrimEnd('/'), StringComparison.OrdinalIgnoreCase));
    }

    private int RegistrarRequisicao(string cacheKey, int janelaSegundos)
    {
        var agora = DateTime.UtcNow;
        var janela = TimeSpan.FromSeconds(Math.Max(1, janelaSegundos));

        var timestamps = _cache.GetOrCreate(cacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = janela + TimeSpan.FromSeconds(5);
            return new List<DateTime>();
        })!;

        lock (timestamps)
        {
            timestamps.RemoveAll(t => agora - t > janela);
            timestamps.Add(agora);
            return timestamps.Count;
        }
    }

    private async Task<IpBurstRateLimitResult> RegistrarBloqueioDuroAsync(
        string ip,
        bool sensivel,
        CancellationToken cancellationToken)
    {
        var blockedUntil = DateTime.UtcNow.AddHours(Math.Max(1, _options.BlockDurationHours));
        await _blockRepository.RegistrarBloqueioAsync(new IpRateLimitBlock
        {
            Ip = ip,
            BlockedUntil = blockedUntil,
            Reason = sensivel ? "IP_SENSITIVE_AUTH_BLOCKED" : "IP_BURST_BLOCKED"
        }, cancellationToken);

        return new IpBurstRateLimitResult(false, true, blockedUntil, ErrorCodeHardBlock);
    }
}
