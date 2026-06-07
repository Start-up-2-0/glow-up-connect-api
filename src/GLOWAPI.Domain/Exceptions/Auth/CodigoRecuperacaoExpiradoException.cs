namespace GLOWAPI.Domain.Exceptions.Auth;

public class CodigoRecuperacaoExpiradoException : DomainException
{
    public const string ErrorCode = "CODIGO_RECUPERACAO_EXPIRADO";
    public CodigoRecuperacaoExpiradoException()
        : base("Código inválido ou expirado.", ErrorCode) { }
}
