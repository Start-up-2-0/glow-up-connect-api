using GLOWAPI.Domain.Exceptions;

namespace GLOWAPI.Domain.Exceptions.Negocios;

public class AtendimentoStatusInvalidoException : DomainException
{
    public const string ErrorCode = "ATENDIMENTO_STATUS_INVALIDO";

    public AtendimentoStatusInvalidoException(string message)
        : base(message, ErrorCode)
    {
    }
}
