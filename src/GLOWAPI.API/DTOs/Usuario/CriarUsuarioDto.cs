using System.ComponentModel.DataAnnotations;
using GLOWAPI.Domain.Enums;

namespace GLOWAPI.API.DTOs.Usuario;

public class CriarUsuarioDto
{
    [Required(ErrorMessage = "Nome é obrigatório")]
    [StringLength(150, MinimumLength = 3, ErrorMessage = "Nome deve ter entre 3 e 150 caracteres")]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email é obrigatório")]
    [EmailAddress(ErrorMessage = "Email inválido")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Telefone é obrigatório")]
    [Phone(ErrorMessage = "Telefone inválido")]
    [StringLength(20, ErrorMessage = "Telefone deve ter no máximo 20 caracteres")]
    public string Telefone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Senha é obrigatória")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Senha deve ter entre 6 e 100 caracteres")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{6,}$",
        ErrorMessage = "Senha deve conter: letra maiúscula, minúscula, número e caractere especial")]
    public string Senha { get; set; } = string.Empty;

    [Required(ErrorMessage = "Cargo é obrigatório")]
    public UserRole Role { get; set; }
}
