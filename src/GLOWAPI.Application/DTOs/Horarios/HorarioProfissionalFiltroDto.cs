namespace GLOWAPI.Application.DTOs.Horarios;

public class HorarioProfissionalFiltroDto
{
    public int? ProfissionalId { get; set; }
    public DayOfWeek? DiaSemana { get; set; }
    public bool? Ativo { get; set; }
}
