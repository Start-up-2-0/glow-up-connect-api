namespace GLOWAPI.Application.DTOs.Horarios;

public class CriarHorarioProfissionalRequestDto
{
    public DayOfWeek DiaSemana { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFim { get; set; }
}
