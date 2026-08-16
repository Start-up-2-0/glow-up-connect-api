using GLOWAPI.Application.DTOs.Usuario;
using System.ComponentModel.DataAnnotations;

namespace GLOWAPI.Application.DTOs.Convites;

public class AceitarConviteComCadastroRequestDto
{
    [Required]
    public CadastrarClienteDto Cadastro { get; set; } = new();
}
