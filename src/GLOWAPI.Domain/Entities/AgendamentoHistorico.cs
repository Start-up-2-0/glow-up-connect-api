using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Domain.Entities;

public class AgendamentoHistorico
{
    public int Id { get; set; }
    public int AgendamentoId { get; set; }
    public int? UsuarioExecutorId { get; set; }
    public AgendamentoStatus StatusAnterior { get; set; }
    public AgendamentoStatus StatusNovo { get; set; }
    public string? Motivo { get; set; }
    public string PayloadJson { get; set; } = "{}";
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public Agendamento? Agendamento { get; set; }
    public Usuario? UsuarioExecutor { get; set; }
}
