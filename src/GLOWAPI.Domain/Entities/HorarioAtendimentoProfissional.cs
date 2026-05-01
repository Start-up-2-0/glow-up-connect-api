namespace GLOWAPI.Domain.Entities;

public class HorarioAtendimentoProfissional
{
    public int Id { get; set; }
    public int ProfissionalId { get; set; }
    public int? EstabelecimentoId { get; set; }
    public DayOfWeek DiaSemana { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFim { get; set; }
    public bool Ativo { get; set; } = true;
    public DateTime CreateAd { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Profissional? Profissional { get; set; }
    public Estabelecimento? Estabelecimento { get; set; }
}
