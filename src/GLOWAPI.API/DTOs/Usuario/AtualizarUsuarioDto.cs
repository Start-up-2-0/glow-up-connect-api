using System.ComponentModel.DataAnnotations;

namespace GLOWAPI.API.DTOs.Usuario;

public class AtualizarUsuarioDto
{
    [Required(ErrorMessage = "Nome é obrigatório")]
    [StringLength(150, MinimumLength = 3, ErrorMessage = "Nome deve ter entre 3 e 150 caracteres")]
    public string Nome { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Telefone inválido")]
    [StringLength(20, ErrorMessage = "Telefone deve ter no máximo 20 caracteres")]
    public string Telefone { get; set; } = string.Empty;
}
