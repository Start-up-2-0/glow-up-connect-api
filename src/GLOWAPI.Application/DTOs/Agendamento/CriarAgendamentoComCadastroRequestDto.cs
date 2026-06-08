using GLOWAPI.Application.DTOs.Usuario;

namespace GLOWAPI.Application.DTOs.Agendamento;

public class CriarAgendamentoComCadastroRequestDto
{
    public Guid ProfissionalPublicGuid { get; set; }
    public int[] ServicoIds { get; set; } = [];
    public DateOnly Data { get; set; }
    public TimeOnly HorarioInicio { get; set; }
    public string? Observacao { get; set; }
    public CadastrarClienteDto Cadastro { get; set; } = new();
}
