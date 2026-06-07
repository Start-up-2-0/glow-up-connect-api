namespace GLOWAPI.Domain.Exceptions.Auth;

public class ResetTokenExpiradoException : DomainException
{
    public const string ErrorCode = "RESET_TOKEN_EXPIRADO";
    public ResetTokenExpiradoException()
        : base("Token de redefinição inválido ou expirado.", ErrorCode) { }
}
