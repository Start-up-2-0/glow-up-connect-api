namespace GLOWAPI.Application.DTOs.Horarios;

public class AgendamentoFuturoImpactadoDto
{
    public int AgendamentoItemId { get; init; }
    public int AgendamentoId { get; init; }
    public int ProfissionalId { get; init; }
    public DateTime Inicio { get; init; }
    public DateTime Fim { get; init; }
}
