using GLOWAPI.Domain.Exceptions;

namespace GLOWAPI.Domain.Exceptions.Negocios;

public class HorarioAlteracaoImpactaAgendamentosFuturosException : DomainException
{
    public const string ErrorCode = "HORARIO_IMPACTA_AGENDAMENTOS_FUTUROS";

    public HorarioAlteracaoImpactaAgendamentosFuturosException(
        string mensagem,
        IReadOnlyList<AgendamentoFuturoImpactadoResumo> agendamentosImpactados)
        : base(mensagem, ErrorCode)
    {
        AgendamentosImpactados = agendamentosImpactados;
    }

    public IReadOnlyList<AgendamentoFuturoImpactadoResumo> AgendamentosImpactados { get; }
}

public record AgendamentoFuturoImpactadoResumo(
    int AgendamentoItemId,
    int AgendamentoId,
    int ProfissionalId,
    DateTime Inicio,
    DateTime Fim);
