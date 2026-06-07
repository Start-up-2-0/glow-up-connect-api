using GLOWAPI.Application.DTOs.Auth;
using GLOWAPI.Application.Models.Auth;

namespace GLOWAPI.Application.Interfaces.Services;

public interface IAuthService
{
    Task<AuthLoginResult> LoginAsync(
        LoginRequestDto dto,
        AuthSessionContext context,
        CancellationToken cancellationToken = default);

    Task LogoutAsync(string accessToken, CancellationToken cancellationToken = default);

    Task<AuthRefreshResult> RefreshAsync(
        RefreshTokenRequestDto dto,
        AuthSessionContext context,
        CancellationToken cancellationToken = default);

    // New Methods 
    Task ForgotPasswordAsync(
        ForgotPasswordRequestDto dto,
        string? ip,
        CancellationToken cancellationToken = default);

    Task<VerifyRecoveryCodeResult> VerifyRecoveryCodeAsync(
        VerifyRecoveryCodeRequestDto dto,
        CancellationToken cancellationToken = default);

    Task ResetPasswordAsync(
        ResetPasswordRequestDto dto,
        CancellationToken cancellationToken = default);

}
