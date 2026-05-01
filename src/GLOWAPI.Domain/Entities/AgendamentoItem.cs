using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Domain.Entities;

public class AgendamentoItem
{
    public int Id { get; set; }
    public int AgendamentoId { get; set; }
    public int ServicoId { get; set; }
    public int ProfissionalId { get; set; }
    public DateTime Inicio { get; set; }
    public DateTime Fim { get; set; }
    public decimal Valor { get; set; }
    public AgendamentoItemStatus Status { get; set; } = AgendamentoItemStatus.Pendente;
    public int? RepassadoDeProfissionalId { get; set; }
    public DateTime CreateAd { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Agendamento? Agendamento { get; set; }
    public Servico? Servico { get; set; }
    public Profissional? Profissional { get; set; }
    public Profissional? RepassadoDeProfissional { get; set; }
}
