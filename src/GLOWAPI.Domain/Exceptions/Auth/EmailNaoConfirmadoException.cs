namespace GLOWAPI.Domain.Exceptions.Auth;

public class EmailNaoConfirmadoException : AuthenticationException
{
    public const string ErrorCode = "EMAIL_NAO_CONFIRMADO";

    public EmailNaoConfirmadoException()
        : base("Confirme seu e-mail antes de entrar.", ErrorCode)
    {
    }
}
