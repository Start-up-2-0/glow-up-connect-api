using GLOWAPI.Domain.Enums;

namespace GLOWAPI.API.DTOs.Usuario;

public class UsuarioResponseDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Telefone { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool Ativo { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
