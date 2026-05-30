using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Application.DTOs.Equipe;

public class CadastrarUsuarioEquipeRequestDto
{
    public string? Email { get; set; }
    public string? Telefone { get; set; }
    public EstablishmentUserRole Role { get; set; }
}
