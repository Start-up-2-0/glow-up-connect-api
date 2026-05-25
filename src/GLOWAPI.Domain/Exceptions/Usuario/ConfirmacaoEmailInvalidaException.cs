namespace GLOWAPI.Domain.Exceptions.Usuario;

public class ConfirmacaoEmailInvalidaException : DomainException
{
    public const string ErrorCode = "CONFIRMACAO_EMAIL_INVALIDA";

    public ConfirmacaoEmailInvalidaException()
        : base("Link ou codigo de confirmacao invalido ou expirado.", ErrorCode)
    {
    }
}
