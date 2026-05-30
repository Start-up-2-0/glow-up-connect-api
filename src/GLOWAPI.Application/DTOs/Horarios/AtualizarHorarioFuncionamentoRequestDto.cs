namespace GLOWAPI.Application.DTOs.Horarios;

public class AtualizarHorarioFuncionamentoRequestDto
{
    public DayOfWeek DiaSemana { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFim { get; set; }
}
