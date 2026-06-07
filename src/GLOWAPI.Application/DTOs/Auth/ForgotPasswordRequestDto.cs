using System.ComponentModel.DataAnnotations;

namespace GLOWAPI.Application.DTOs.Auth;

public class ForgotPasswordRequestDto
{
    ///<summary>
    /// E-mail
    ///</summary>
    [Required(ErrorMessage = "O e-mail é obrigatório.")]
    [EmailAddress(ErrorMessage = "E-mail Inválido.")]
    public string Email { get; set; } = string.Empty;
}