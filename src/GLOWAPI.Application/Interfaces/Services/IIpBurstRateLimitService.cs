namespace GLOWAPI.Application.Interfaces.Services;

public interface IIpBurstRateLimitService
{
    Task<IpBurstRateLimitResult> AvaliarAsync(string ip, CancellationToken cancellationToken = default);
}

public record IpBurstRateLimitResult(bool Permitido, bool BloqueadoPorBurst, DateTime? BlockedUntil);
