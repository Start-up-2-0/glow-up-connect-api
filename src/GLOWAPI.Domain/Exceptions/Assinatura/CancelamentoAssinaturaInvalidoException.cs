using GLOWAPI.Domain.Exceptions;

namespace GLOWAPI.Domain.Exceptions.Assinatura;

public class CancelamentoAssinaturaInvalidoException : DomainException
{
    public const string ErrorCode = "CANCELAMENTO_ASSINATURA_INVALIDO";

    public CancelamentoAssinaturaInvalidoException(string message)
        : base(message, ErrorCode)
    {
    }
}
