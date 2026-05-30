using GLOWAPI.Domain.Exceptions;

namespace GLOWAPI.Domain.Exceptions.Negocios;

public class HorarioAtendimentoNaoEncontradoException : DomainException
{
    public const string ErrorCode = "HORARIO_ATENDIMENTO_NAO_ENCONTRADO";

    public HorarioAtendimentoNaoEncontradoException()
        : base("Horario de atendimento do profissional nao encontrado.", ErrorCode)
    {
    }
}
