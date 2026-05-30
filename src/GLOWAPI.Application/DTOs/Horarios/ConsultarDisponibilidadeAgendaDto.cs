namespace GLOWAPI.Application.DTOs.Horarios;

public class ConsultarDisponibilidadeAgendaDto
{
    public DateOnly DataInicio { get; set; }
    public DateOnly DataFim { get; set; }
    public int ServicoId { get; set; }
    public int? ProfissionalId { get; set; }
}
