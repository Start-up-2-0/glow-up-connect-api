using GLOWAPI.Application.DTOs.Usuario;
using System.ComponentModel.DataAnnotations;

namespace GLOWAPI.Application.DTOs.Convites;

public class AceitarConviteComCadastroRequestDto
{
    [Required]
    public CadastrarClienteDto Cadastro { get; set; } = new();

    /// <summary>Nome público do profissional (quando o convite for de profissional).</summary>
    [StringLength(150, MinimumLength = 2, ErrorMessage = "Nome publico deve ter entre 2 e 150 caracteres")]
    public string? NomePublico { get; set; }

    /// <summary>Foto de apresentação do profissional (data URI/base64).</summary>
    public string? Foto { get; set; }

    public string? FotoContentType { get; set; }
}
