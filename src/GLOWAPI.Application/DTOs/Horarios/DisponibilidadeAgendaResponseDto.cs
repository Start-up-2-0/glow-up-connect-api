namespace GLOWAPI.Application.DTOs.Horarios;

public class DisponibilidadeAgendaResponseDto
{
    public int ServicoId { get; init; }
    public int[] ServicoIds { get; init; } = [];
    public int DuracaoMinutos { get; init; }
    public string? MensagemIndisponibilidade { get; init; }
    public IReadOnlyList<DateOnly> DatasAtendimento { get; init; } = [];
    public IReadOnlyList<SlotDisponivelResponseDto> Slots { get; init; } = [];
}
