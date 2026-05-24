using GLOWAPI.Application.Models.Auth;

namespace GLOWAPI.Application.DTOs.Auth;

public class RefreshTokenResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime RefreshExpiresAt { get; set; }

    public static RefreshTokenResponseDto From(AuthRefreshResult result) => new()
    {
        Token = result.Token,
        RefreshToken = result.RefreshToken,
        ExpiresAt = result.ExpiresAt,
        RefreshExpiresAt = result.RefreshExpiresAt
    };
}
