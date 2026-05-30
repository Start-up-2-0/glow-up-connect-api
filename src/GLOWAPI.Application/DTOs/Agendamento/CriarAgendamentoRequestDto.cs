namespace GLOWAPI.Application.DTOs.Agendamento;

public class CriarAgendamentoRequestDto
{
    public Guid? ProfissionalPublicGuid { get; set; }
    public int[] ServicoIds { get; set; } = [];
    public DateOnly Data { get; set; }
    public TimeOnly HorarioInicio { get; set; }
    public string? ClienteNome { get; set; }
    public string? ClienteEmail { get; set; }
    public string? ClienteTelefone { get; set; }
    public string? Observacao { get; set; }
}
