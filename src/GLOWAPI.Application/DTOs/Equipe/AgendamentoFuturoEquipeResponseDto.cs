namespace GLOWAPI.Application.DTOs.Equipe;

public class AgendamentoFuturoEquipeResponseDto
{
    public int AgendamentoId { get; init; }
    public int AgendamentoItemId { get; init; }
    public string ClienteNome { get; init; } = string.Empty;
    public string ServicoNome { get; init; } = string.Empty;
    public DateTime Inicio { get; init; }
    public DateTime Fim { get; init; }
    public string Status { get; init; } = string.Empty;
}
