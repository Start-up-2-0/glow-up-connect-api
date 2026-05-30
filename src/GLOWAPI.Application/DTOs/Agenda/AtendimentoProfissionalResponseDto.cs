using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.DTOs.Agenda;

public record AtendimentoProfissionalResponseDto(
    int AgendamentoItemId,
    int AgendamentoId,
    string StatusItem,
    string StatusAgendamento,
    DateTime? AtualizadoEm)
{
    public static AtendimentoProfissionalResponseDto From(AgendamentoItem item) =>
        new(
            item.Id,
            item.AgendamentoId,
            item.Status.ToString(),
            item.Agendamento?.Status.ToString() ?? string.Empty,
            item.UpdatedAt);
}
