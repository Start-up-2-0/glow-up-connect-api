using GLOWAPI.Application.DTOs.Agendamento;
using GLOWAPI.Application.Models.Agendamento;
using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Application.Interfaces.Services;

public interface IAgendamentoValidador
{
    Task<AgendamentoPreparacaoResultado> PrepararAsync(
        int estabelecimentoId,
        int profissionalId,
        int[] servicoIds,
        DateOnly data,
        TimeOnly horarioInicio,
        OrigemAgendamento origem,
        CriarAgendamentoRequestDto? dadosVisitante,
        int? usuarioClienteId,
        int? agendamentoIgnorarId = null,
        CancellationToken cancellationToken = default);

    Task ValidarConflitoAsync(
        int estabelecimentoId,
        int profissionalId,
        DateTime inicio,
        DateTime fim,
        int? agendamentoIgnorarId,
        CancellationToken cancellationToken = default);
}
