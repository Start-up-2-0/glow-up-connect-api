using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Domain.Entities;

public class AgendamentoPropostaRemarcacao
{
    public int Id { get; set; }
    public int AgendamentoId { get; set; }
    public DateOnly DataSugerida { get; set; }
    public TimeOnly HorarioInicioSugerido { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public PropostaRemarcacaoStatus Status { get; set; } = PropostaRemarcacaoStatus.Pendente;
    public Guid TokenPublico { get; set; }
    public DateTime ExpiraEm { get; set; }
    public DateTime? RespondidoEm { get; set; }
    public int? UsuarioExecutorId { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public Agendamento? Agendamento { get; set; }
    public Usuario? UsuarioExecutor { get; set; }
}
