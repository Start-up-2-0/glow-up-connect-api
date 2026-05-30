namespace GLOWAPI.Application.DTOs.Horarios;

public class DisponibilidadeAgendaResponseDto
{
    public int ServicoId { get; init; }
    public int DuracaoMinutos { get; init; }
    public IReadOnlyList<SlotDisponivelResponseDto> Slots { get; init; } = [];
}
