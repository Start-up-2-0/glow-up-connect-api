using System.ComponentModel.DataAnnotations;

namespace GLOWAPI.Application.DTOs.Auth;

public class ResetPasswordRequestDto
{
    [Required(ErrorMessage = "O token de redefinição é obrigatório.")]
    public string ResetToken { get; set; } = string.Empty;

    [Required(ErrorMessage = "A nova senha é obrigatória.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "A senha deve ter entre 6 e 100 caracteres.")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{6,100}$",
        ErrorMessage = "A senha deve conter maiúscula, minúscula, número e caractere especial.")]
    public string NovaSenha { get; set; } = string.Empty;
}
