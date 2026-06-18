using GLOWAPI.Application.DTOs.Avaliacao;
using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.Interfaces.Services;

public interface IAvaliacaoAtendimentoService
{
    Task<AvaliacaoContextoResponseDto> ObterContextoMeuAgendamentoAsync(
        int agendamentoId,
        CancellationToken cancellationToken = default);

    Task<AvaliacaoContextoResponseDto> ObterContextoPorTokenAsync(
        Guid token,
        CancellationToken cancellationToken = default);

    Task<AvaliacaoContextoResponseDto> CriarMeuAgendamentoAsync(
        int agendamentoId,
        CriarAvaliacaoAtendimentoRequestDto request,
        CancellationToken cancellationToken = default);

    Task<AvaliacaoContextoResponseDto> CriarPorTokenAsync(
        Guid token,
        CriarAvaliacaoAtendimentoRequestDto request,
        CancellationToken cancellationToken = default);

    Task SolicitarAposConclusaoAsync(
        Agendamento agendamento,
        CancellationToken cancellationToken = default);

    Task<string> ObterStatusAvaliacaoAsync(
        Agendamento agendamento,
        CancellationToken cancellationToken = default);

    Task<AvaliacaoResumoClienteDto?> ObterResumoClienteAsync(
        int agendamentoId,
        CancellationToken cancellationToken = default);
}
