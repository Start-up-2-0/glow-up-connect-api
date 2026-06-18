namespace GLOWAPI.Application.Interfaces.Services;

public interface IIpBurstRateLimitService
{
    Task<IpBurstRateLimitResult> AvaliarAsync(
        string ip,
        string path,
        bool possuiTokenAutenticacao,
        CancellationToken cancellationToken = default);
}

public record IpBurstRateLimitResult(
    bool Permitido,
    bool BloqueadoPorBurst,
    DateTime? BlockedUntil,
    string? ErrorCode = null,
    int? RetryAfterSeconds = null);
