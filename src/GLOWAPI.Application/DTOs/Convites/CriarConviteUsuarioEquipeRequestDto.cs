using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Application.DTOs.Convites;

public class CriarConviteUsuarioEquipeRequestDto
{
    public string Email { get; set; } = string.Empty;
    public EstablishmentUserRole Role { get; set; }
}
