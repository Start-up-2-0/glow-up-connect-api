using GLOWAPI.Domain.Exceptions;

namespace GLOWAPI.Domain.Exceptions.Negocios;

public class HorarioAtendimentoConflitanteException : DomainException
{
    public const string ErrorCode = "HORARIO_ATENDIMENTO_CONFLITANTE";

    public HorarioAtendimentoConflitanteException(string? mensagem = null)
        : base(mensagem ?? "Ja existe horario ativo conflitante neste periodo.", ErrorCode)
    {
    }
}
