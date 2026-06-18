namespace GLOWAPI.Application.Interfaces.Services;

public interface ISecurityAuditLogger
{
    Task LoginSucceededAsync(int usuarioId, string email, string? ip, string? userAgent, CancellationToken cancellationToken = default);
    Task LoginFailedAsync(string email, string reason, string? ip, string? userAgent, int? usuarioId = null, CancellationToken cancellationToken = default);
    Task UserBlockedAsync(int usuarioId, string email, string? ip, string? userAgent, CancellationToken cancellationToken = default);
    Task LogoutAsync(int usuarioId, int sessionId, string? ip, string? userAgent, CancellationToken cancellationToken = default);
    Task TokenRefreshedAsync(int usuarioId, int sessionId, string? ip, string? userAgent, CancellationToken cancellationToken = default);
    Task AccessDeniedAsync(string reason, string? ip, string? userAgent, int? usuarioId = null, CancellationToken cancellationToken = default);
    Task IpBurstBlockedAsync(string ip, string? userAgent, CancellationToken cancellationToken = default);
}
