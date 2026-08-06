using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Application.Models.Auth;

public record UsuarioAuthInfo(int Id, string Nome, string Email, UserRole Role, string? AvatarBase64 = null);

public record AuthLoginResult(
    string Token,
    string RefreshToken,
    DateTime ExpiresAt,
    DateTime RefreshExpiresAt,
    UsuarioAuthInfo Usuario,
    bool RequerConfirmacaoEmail = false,
    bool SessaoAgendamentoPublico = false);

public record AuthRefreshResult(
    string Token,
    string RefreshToken,
    DateTime ExpiresAt,
    DateTime RefreshExpiresAt);

public record IssuedTokenPair(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAt,
    DateTime RefreshTokenExpiresAt,
    int SessionId);

public record AuthSessionContext(string? Ip, string? UserAgent);

public record AuthenticatedSessionResult(
    SessaoAutenticacaoInfo Sessao,
    UsuarioAuthInfo Usuario);

public record SessaoAutenticacaoInfo(int Id, int UsuarioId, string? Ip, string? UserAgent);
