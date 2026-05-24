using GLOWAPI.Application.Models.Auth;

namespace GLOWAPI.Application.Interfaces.Services;

public interface IAuthService
{
    Task<AuthLoginResult> LoginAsync(
        string email,
        string senha,
        AuthSessionContext context,
        CancellationToken cancellationToken = default);

    Task LogoutAsync(string accessToken, CancellationToken cancellationToken = default);

    Task<AuthRefreshResult> RefreshAsync(
        string refreshToken,
        AuthSessionContext context,
        CancellationToken cancellationToken = default);
}
