namespace GLOWAPI.Domain.Exceptions.Assinatura;

public class PagamentoAssinaturaInvalidoException : DomainException
{
    public const string ErrorCode = "PAGAMENTO_ASSINATURA_INVALIDO";

    public PagamentoAssinaturaInvalidoException(string message)
        : base(message, ErrorCode)
    {
    }
}
