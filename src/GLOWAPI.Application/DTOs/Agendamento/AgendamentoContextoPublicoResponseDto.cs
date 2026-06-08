using GLOWAPI.Application.DTOs.Estabelecimentos;

namespace GLOWAPI.Application.DTOs.Agendamento;

public class AgendamentoContextoPublicoResponseDto
{
    public EstabelecimentoPublicoResponseDto Estabelecimento { get; set; } = null!;
    public ProfissionalPublicoResponseDto Profissional { get; set; } = null!;
    public bool PodeReceberAgendamento { get; set; }
}
