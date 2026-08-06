using GLOWAPI.Application.DTOs.Auth;
using GLOWAPI.Application.Models.Auth;

namespace GLOWAPI.Application.Interfaces.Services;

public interface IAuthService
{
    Task<AuthLoginResult> LoginAsync(
        LoginRequestDto dto,
        AuthSessionContext context,
        CancellationToken cancellationToken = default);

    Task<AuthLoginResult> AutenticarPorCodigoAgendamentoAsync(
        AutenticarCodigoAgendamentoRequestDto dto,
        AuthSessionContext context,
        CancellationToken cancellationToken = default);

    Task LogoutAsync(string accessToken, CancellationToken cancellationToken = default);

    Task<AuthRefreshResult> RefreshAsync(
        RefreshTokenRequestDto dto,
        AuthSessionContext context,
        CancellationToken cancellationToken = default);
}
