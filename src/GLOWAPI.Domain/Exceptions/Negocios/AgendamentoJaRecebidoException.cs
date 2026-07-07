using GLOWAPI.Domain.Exceptions;

namespace GLOWAPI.Domain.Exceptions.Negocios;

public class AgendamentoJaRecebidoException : DomainException
{
    public const string ErrorCode = "AGENDAMENTO_JA_RECEBIDO";

    public AgendamentoJaRecebidoException()
        : base("Este agendamento ja foi recebido.", ErrorCode)
    {
    }
}
