using System.ComponentModel.DataAnnotations;

namespace GLOWAPI.Application.DTOs.Usuario;

public class CadastrarClienteDto
{
    [Required(ErrorMessage = "Nome e obrigatorio")]
    [StringLength(150, MinimumLength = 3, ErrorMessage = "Nome deve ter entre 3 e 150 caracteres")]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email e obrigatorio")]
    [EmailAddress(ErrorMessage = "Email invalido")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Telefone e obrigatorio")]
    [Phone(ErrorMessage = "Telefone invalido")]
    [StringLength(20, ErrorMessage = "Telefone deve ter no maximo 20 caracteres")]
    public string Telefone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Senha e obrigatoria")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Senha deve ter entre 6 e 100 caracteres")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{6,}$",
        ErrorMessage = "Senha deve conter: letra maiuscula, minuscula, numero e caractere especial")]
    public string Senha { get; set; } = string.Empty;

    public string? AvatarBase64 { get; set; }

    public string? AvatarContentType { get; set; }

    public string? CaptchaToken { get; set; }
}
