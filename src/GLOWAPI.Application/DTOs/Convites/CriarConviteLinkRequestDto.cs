using System.ComponentModel.DataAnnotations;
using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Application.DTOs.Convites;

public class CriarConviteLinkRequestDto
{
    [Required]
    public EstablishmentUserRole Role { get; set; }

    [Required]
    [Range(1, 100)]
    public int LimiteUsuarios { get; set; } = 1;

    /// <summary>Null = padrão 1 dia.</summary>
    [Range(1, 3650)]
    public int? DuracaoValor { get; set; }

    public UnidadeDuracaoConvite? DuracaoUnidade { get; set; }

    public bool PodeReceberAgendamento { get; set; } = true;
}
