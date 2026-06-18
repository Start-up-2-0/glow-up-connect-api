namespace GLOWAPI.Application.DTOs.Agendamento;

public class RemarcarAgendamentoRequestDto
{
    public DateOnly Data { get; set; }
    public TimeOnly HorarioInicio { get; set; }
    public DateTime? InicioSelecionado { get; set; }
    public string Motivo { get; set; } = string.Empty;
}
