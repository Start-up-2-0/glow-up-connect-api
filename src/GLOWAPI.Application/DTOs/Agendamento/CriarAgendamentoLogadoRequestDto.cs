namespace GLOWAPI.Application.DTOs.Agendamento;

public class CriarAgendamentoLogadoRequestDto
{
    public Guid EstabelecimentoPublicGuid { get; set; }
    public Guid? ProfissionalPublicGuid { get; set; }
    public int[] ServicoIds { get; set; } = [];
    public DateOnly Data { get; set; }
    public TimeOnly HorarioInicio { get; set; }
    public string? Observacao { get; set; }
}
