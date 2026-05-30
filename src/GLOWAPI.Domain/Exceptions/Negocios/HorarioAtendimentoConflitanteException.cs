using GLOWAPI.Domain.Exceptions;

namespace GLOWAPI.Domain.Exceptions.Negocios;

public class HorarioAtendimentoConflitanteException : DomainException
{
    public const string ErrorCode = "HORARIO_ATENDIMENTO_CONFLITANTE";

    public HorarioAtendimentoConflitanteException()
        : base("O profissional ja possui horario ativo conflitante neste periodo.", ErrorCode)
    {
    }
}
