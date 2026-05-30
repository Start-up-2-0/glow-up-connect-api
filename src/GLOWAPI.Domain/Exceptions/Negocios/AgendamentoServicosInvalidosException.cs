using GLOWAPI.Domain.Exceptions;

namespace GLOWAPI.Domain.Exceptions.Negocios;

public class AgendamentoServicosInvalidosException : DomainException
{
    public const string ErrorCode = "AGENDAMENTO_SERVICOS_INVALIDOS";

    public AgendamentoServicosInvalidosException()
        : base("Servicos informados sao invalidos para o agendamento.", ErrorCode)
    {
    }

    public AgendamentoServicosInvalidosException(string message)
        : base(message, ErrorCode)
    {
    }
}
