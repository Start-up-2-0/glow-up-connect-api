namespace GLOWAPI.Domain.Exceptions.Auth;

public class ResetTokenInvalidoException : DomainException
{
    public const string ErrorCode = "RESET_TOKEN_INVALIDO";
    public ResetTokenInvalidoException()
        : base("Token de redefinição inválido ou expirado.", ErrorCode) { }
}
