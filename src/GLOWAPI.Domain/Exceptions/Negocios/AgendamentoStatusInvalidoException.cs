using GLOWAPI.Domain.Exceptions;

namespace GLOWAPI.Domain.Exceptions.Negocios;

public class AgendamentoStatusInvalidoException : DomainException
{
    public const string ErrorCode = "AGENDAMENTO_STATUS_INVALIDO";

    public AgendamentoStatusInvalidoException()
        : base("Status do agendamento invalido para esta operacao.", ErrorCode)
    {
    }

    public AgendamentoStatusInvalidoException(string message)
        : base(message, ErrorCode)
    {
    }
}
