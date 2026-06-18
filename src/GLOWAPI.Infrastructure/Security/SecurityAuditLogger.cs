using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace GLOWAPI.Infrastructure.Security;

public class SecurityAuditLogger : ISecurityAuditLogger
{
    private readonly ILogger<SecurityAuditLogger> _logger;
    private readonly ILogAutenticacaoRepository _logRepository;

    public SecurityAuditLogger(ILogger<SecurityAuditLogger> logger, ILogAutenticacaoRepository logRepository)
    {
        _logger = logger;
        _logRepository = logRepository;
    }

    public Task LoginSucceededAsync(int usuarioId, string email, string? ip, string? userAgent, CancellationToken cancellationToken = default) =>
        RegistrarAsync("LOGIN_OK", email, ip, userAgent, usuarioId, null, cancellationToken);

    public Task LoginFailedAsync(string email, string reason, string? ip, string? userAgent, int? usuarioId = null, CancellationToken cancellationToken = default) =>
        RegistrarAsync("LOGIN_FAIL", email, ip, userAgent, usuarioId, reason, cancellationToken);

    public Task UserBlockedAsync(int usuarioId, string email, string? ip, string? userAgent, CancellationToken cancellationToken = default) =>
        RegistrarAsync("LOCKOUT", email, ip, userAgent, usuarioId, null, cancellationToken);

    public Task LogoutAsync(int usuarioId, int sessionId, string? ip, string? userAgent, CancellationToken cancellationToken = default) =>
        RegistrarAsync("LOGOUT", string.Empty, ip, userAgent, usuarioId, $"sessionId={sessionId}", cancellationToken);

    public Task TokenRefreshedAsync(int usuarioId, int sessionId, string? ip, string? userAgent, CancellationToken cancellationToken = default) =>
        RegistrarAsync("REFRESH", string.Empty, ip, userAgent, usuarioId, $"sessionId={sessionId}", cancellationToken);

    public Task AccessDeniedAsync(string reason, string? ip, string? userAgent, int? usuarioId = null, CancellationToken cancellationToken = default) =>
        RegistrarAsync("ACCESS_DENIED", string.Empty, ip, userAgent, usuarioId, reason, cancellationToken);

    public Task IpBurstBlockedAsync(string ip, string? userAgent, CancellationToken cancellationToken = default) =>
        RegistrarAsync("IP_BURST_BLOCKED", string.Empty, ip, userAgent, null, "burst_excedido", cancellationToken);

    private async Task RegistrarAsync(
        string evento,
        string email,
        string? ip,
        string? userAgent,
        int? usuarioId,
        string? detalhes,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Auth audit {Evento}. UsuarioId={UsuarioId}, Email={Email}, Ip={Ip}, Detalhes={Detalhes}",
            evento, usuarioId, email, ip, detalhes);

        await _logRepository.AdicionarAsync(new LogAutenticacao
        {
            UsuarioId = usuarioId,
            Email = email,
            Evento = evento,
            Ip = ip,
            UserAgent = userAgent,
            Detalhes = detalhes
        }, cancellationToken);

        await _logRepository.SalvarAlteracoesAsync(cancellationToken);
    }
}
