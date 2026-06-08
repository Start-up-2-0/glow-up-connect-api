using GLOWAPI.Application.DTOs.Agendamento;
using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Application.Interfaces.Services;

public interface IAgendamentoNegocioService
{
    Task<IReadOnlyList<ProfissionalPublicoResponseDto>> ListarProfissionaisPublicosPorLojaAsync(
        Guid publicGuidLoja,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProfissionalVitrinePublicoResponseDto>> ListarProfissionaisVitrinePorLojaAsync(
        Guid publicGuidLoja,
        CancellationToken cancellationToken = default);

    Task<AgendamentoCriadoResponseDto> CriarPublicoPorLojaAsync(
        Guid publicGuidLoja,
        CriarAgendamentoRequestDto request,
        CancellationToken cancellationToken = default);

    Task<AgendamentoCriadoResponseDto> CriarPublicoPorProfissionalAsync(
        Guid publicGuidProfissional,
        CriarAgendamentoRequestDto request,
        CancellationToken cancellationToken = default);

    Task<AgendamentoClienteResponseDto> CriarLogadoAsync(
        CriarAgendamentoLogadoRequestDto request,
        CancellationToken cancellationToken = default);

    Task<AgendamentosClientePaginadoResponseDto> ListarMeusAgendamentosAsync(
        AgendamentoClienteFiltroDto filtro,
        CancellationToken cancellationToken = default);

    Task<AgendamentoClienteResponseDto> ObterMeuAgendamentoAsync(
        int agendamentoId,
        CancellationToken cancellationToken = default);

    Task<AgendamentoClienteResponseDto> CancelarMeuAgendamentoAsync(
        int agendamentoId,
        CancelarAgendamentoRequestDto request,
        CancellationToken cancellationToken = default);

    Task<AgendamentoClienteResponseDto> RemarcarMeuAgendamentoAsync(
        int agendamentoId,
        RemarcarAgendamentoRequestDto request,
        CancellationToken cancellationToken = default);

    Task<AgendamentoCriadoResponseDto> ConfirmarAsync(
        int estabelecimentoId,
        int agendamentoId,
        CancellationToken cancellationToken = default);

    Task<AgendamentoCriadoResponseDto> CancelarAsync(
        int estabelecimentoId,
        int agendamentoId,
        CancelarAgendamentoRequestDto request,
        CancellationToken cancellationToken = default);

    Task<AgendamentoCriadoResponseDto> RemarcarAsync(
        int estabelecimentoId,
        int agendamentoId,
        RemarcarAgendamentoRequestDto request,
        CancellationToken cancellationToken = default);

    Task<AgendamentoCriadoResponseDto> MarcarNaoCompareceuAsync(
        int estabelecimentoId,
        int agendamentoId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AgendamentoHistoricoResponseDto>> ObterHistoricoAsync(
        int estabelecimentoId,
        int agendamentoId,
        CancellationToken cancellationToken = default);
}
