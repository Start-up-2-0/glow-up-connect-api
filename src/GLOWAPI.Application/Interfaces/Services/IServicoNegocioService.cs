using GLOWAPI.Application.DTOs.Servicos;

namespace GLOWAPI.Application.Interfaces.Services;

public interface IServicoNegocioService
{
    Task<IReadOnlyList<ServicoResponseDto>> ListarAsync(
        int estabelecimentoId,
        ServicoFiltroDto filtro,
        CancellationToken cancellationToken = default);

    Task<ServicoResponseDto> CriarAsync(
        int estabelecimentoId,
        CriarServicoRequestDto request,
        CancellationToken cancellationToken = default);

    Task<ServicoResponseDto> AtualizarAsync(
        int estabelecimentoId,
        int servicoId,
        AtualizarServicoRequestDto request,
        CancellationToken cancellationToken = default);

    Task<ServicoResponseDto> AtualizarStatusAsync(
        int estabelecimentoId,
        int servicoId,
        AtualizarStatusServicoRequestDto request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ServicoPublicoResponseDto>> ListarPublicosPorEstabelecimentoAsync(
        Guid publicGuid,
        Guid? profissionalPublicGuid = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ServicoPublicoResponseDto>> ListarPublicosPorProfissionalAsync(
        Guid publicGuid,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ServicoPublicoResponseDto>> ListarPublicosPorProfissionalAutonomoAsync(
        Guid publicGuid,
        CancellationToken cancellationToken = default);
}
