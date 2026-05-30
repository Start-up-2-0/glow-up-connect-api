using GLOWAPI.Domain.Exceptions;

namespace GLOWAPI.Domain.Exceptions.Negocios;

public class ProfissionalServicoComAgendamentoFuturoException : DomainException
{
    public const string ErrorCode = "PROFISSIONAL_SERVICO_COM_AGENDAMENTO_FUTURO";

    public ProfissionalServicoComAgendamentoFuturoException()
        : base("Nao e possivel desvincular o profissional enquanto existirem agendamentos futuros confirmados para este servico.", ErrorCode)
    {
    }
}
