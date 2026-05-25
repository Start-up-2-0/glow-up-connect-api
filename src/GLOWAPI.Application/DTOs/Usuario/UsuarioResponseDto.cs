using GLOWAPI.Domain.Enums;
using UsuarioEntity = GLOWAPI.Domain.Entities.Usuario;

namespace GLOWAPI.Application.DTOs.Usuario;

public class UsuarioResponseDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Telefone { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool Ativo { get; set; }
    public string? AvatarBase64 { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public static UsuarioResponseDto From(UsuarioEntity usuario) => new()
    {
        Id = usuario.Id,
        Nome = usuario.Nome,
        Email = usuario.Email,
        Telefone = usuario.Telefone,
        Role = usuario.Role,
        Ativo = usuario.Ativo,
        AvatarBase64 = usuario.AvatarBase64,
        CreatedAt = usuario.CreatedAt,
        UpdatedAt = usuario.UpdatedAt
    };
}
