using System.ComponentModel.DataAnnotations;

namespace GLOWAPI.Application.DTOs.Auth;

public class VerifyRecoveryCodeRequestDto
{
    [Required(ErrorMessage = "O e-mail é obrigatório.")]
    [EmailAddress(ErrorMessage = "E-mail inválido.")]
    public string Email { get; set;} = string.Empty;

    [Required(ErrorMessage = "O código é obrigatório.")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "O código deve ter exatamente 6 dígitos.")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "O código deve conter apenas números.")]
    public string Codigo { get; set;} = string.Empty;
} 