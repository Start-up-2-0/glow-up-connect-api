using GLOWAPI.Domain.Exceptions;

namespace GLOWAPI.Domain.Exceptions.Negocios;

public class ProfissionalEquipeComAgendamentoFuturoException : DomainException
{
    public const string ErrorCode = "PROFISSIONAL_EQUIPE_COM_AGENDAMENTO_FUTURO";

    public ProfissionalEquipeComAgendamentoFuturoException(
        string mensagem,
        IReadOnlyList<AgendamentoFuturoEquipeResumo> agendamentosFuturos)
        : base(mensagem, ErrorCode)
    {
        AgendamentosFuturos = agendamentosFuturos;
    }

    public IReadOnlyList<AgendamentoFuturoEquipeResumo> AgendamentosFuturos { get; }
}

public record AgendamentoFuturoEquipeResumo(
    int AgendamentoId,
    int AgendamentoItemId,
    string ClienteNome,
    string ServicoNome,
    DateTime Inicio,
    DateTime Fim,
    string Status);
