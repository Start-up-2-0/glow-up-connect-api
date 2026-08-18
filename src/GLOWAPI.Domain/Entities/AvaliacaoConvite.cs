namespace GLOWAPI.Domain.Entities;

public class AvaliacaoConvite
{
    public int Id { get; set; }
    public int AgendamentoId { get; set; }
    public Guid TokenPublico { get; set; }
    public DateTime ExpiraEm { get; set; }
    public DateTime? UtilizadoEm { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public Agendamento? Agendamento { get; set; }
}
