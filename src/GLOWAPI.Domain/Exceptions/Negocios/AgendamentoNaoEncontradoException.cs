using GLOWAPI.Domain.Exceptions;

namespace GLOWAPI.Domain.Exceptions.Negocios;

public class AgendamentoNaoEncontradoException : DomainException
{
    public const string ErrorCode = "AGENDAMENTO_NAO_ENCONTRADO";

    public AgendamentoNaoEncontradoException()
        : base("Agendamento nao encontrado.", ErrorCode)
    {
    }
}
