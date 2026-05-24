namespace GLOWAPI.API.DTOs.Auth;

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime RefreshExpiresAt { get; set; }
    public UsuarioAuthDto Usuario { get; set; } = new();
}

public class UsuarioAuthDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public GLOWAPI.Domain.Enums.UserRole Role { get; set; }
}
