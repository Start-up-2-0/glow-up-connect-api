using GLOWAPI.Application.Models.Auth;
using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Application.DTOs.Auth;

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime RefreshExpiresAt { get; set; }
    public UsuarioAuthDto Usuario { get; set; } = new();
    public bool RequerConfirmacaoEmail { get; set; }

    public static LoginResponseDto From(AuthLoginResult result) => new()
    {
        Token = result.Token,
        RefreshToken = result.RefreshToken,
        ExpiresAt = result.ExpiresAt,
        RefreshExpiresAt = result.RefreshExpiresAt,
        Usuario = UsuarioAuthDto.From(result.Usuario),
        RequerConfirmacaoEmail = result.RequerConfirmacaoEmail
    };
}

public class UsuarioAuthDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public string? AvatarBase64 { get; set; }

    public static UsuarioAuthDto From(UsuarioAuthInfo info) => new()
    {
        Id = info.Id,
        Nome = info.Nome,
        Email = info.Email,
        Role = info.Role,
        AvatarBase64 = info.AvatarBase64
    };
}
