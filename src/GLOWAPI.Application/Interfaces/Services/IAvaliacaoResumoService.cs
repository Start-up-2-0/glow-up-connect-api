using GLOWAPI.Application.DTOs.Avaliacao;

namespace GLOWAPI.Application.Interfaces.Services;

public interface IAvaliacaoResumoService
{
    Task<AvaliacaoResumoPublicoDto> ObterResumoEstabelecimentoAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken = default);

    Task<AvaliacaoResumoPublicoDto> ObterResumoProfissionalAsync(
        int profissionalId,
        CancellationToken cancellationToken = default);

    Task<AvaliacoesPaginadasResponseDto> ListarEstabelecimentoPublicoAsync(
        Guid publicGuid,
        int pagina,
        int tamanhoPagina,
        CancellationToken cancellationToken = default);

    Task<AvaliacoesPaginadasResponseDto> ListarProfissionalPublicoAsync(
        Guid publicGuid,
        int pagina,
        int tamanhoPagina,
        CancellationToken cancellationToken = default);

    Task<AvaliacoesNegocioPaginadasResponseDto> ListarNegocioAsync(
        int estabelecimentoId,
        int pagina,
        int tamanhoPagina,
        CancellationToken cancellationToken = default);

    Task RecalcularCacheEstabelecimentoAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken = default);

    Task RecalcularCacheProfissionalAsync(
        int profissionalId,
        CancellationToken cancellationToken = default);

    Task RecalcularCachesDiarioAsync(CancellationToken cancellationToken = default);
}
