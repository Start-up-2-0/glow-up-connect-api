namespace GLOWAPI.Domain.Exceptions.Auth;

public class CodigoRecuperacaoInvalidoException : DomainException
{
    public const string ErrorCode = "CODIGO_RECUPERACAO_INVALIDO";
    public CodigoRecuperacaoInvalidoException()
        : base("Código inválido ou expirado.", ErrorCode) { }
}
