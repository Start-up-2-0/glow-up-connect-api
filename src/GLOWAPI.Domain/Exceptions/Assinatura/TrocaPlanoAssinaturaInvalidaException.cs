using GLOWAPI.Domain.Exceptions;

namespace GLOWAPI.Domain.Exceptions.Assinatura;

public class TrocaPlanoAssinaturaInvalidaException : DomainException
{
    public const string ErrorCode = "TROCA_PLANO_ASSINATURA_INVALIDA";

    public TrocaPlanoAssinaturaInvalidaException(string message)
        : base(message, ErrorCode)
    {
    }
}
