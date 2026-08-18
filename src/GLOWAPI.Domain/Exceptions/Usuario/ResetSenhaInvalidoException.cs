namespace GLOWAPI.Domain.Exceptions.Usuario;

public class ResetSenhaInvalidoException : DomainException
{
    public const string ErrorCode = "RESET_SENHA_INVALIDO";

    public ResetSenhaInvalidoException()
        : base("Link ou codigo de recuperacao invalido ou expirado.", ErrorCode)
    {
    }

    public ResetSenhaInvalidoException(string message)
        : base(message, ErrorCode)
    {
    }
}
