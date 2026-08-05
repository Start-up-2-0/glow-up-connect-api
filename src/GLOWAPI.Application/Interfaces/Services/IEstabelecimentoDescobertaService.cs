using GLOWAPI.Application.DTOs.Estabelecimentos;

namespace GLOWAPI.Application.Interfaces.Services;

public interface IEstabelecimentoDescobertaService
{
    Task<EstabelecimentosProximosPaginadoResponseDto> ListarProximosAsync(
        decimal latitude,
        decimal longitude,
        double? raioKm,
        int pagina,
        int tamanhoPagina,
        int? categoriaId,
        CancellationToken cancellationToken = default);

    Task<EstabelecimentoPublicoResponseDto> ObterPorPublicGuidAsync(
        Guid publicGuid,
        decimal? latitude,
        decimal? longitude,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EstabelecimentoCategoriaDto>> ListarCategoriasAsync(
        CancellationToken cancellationToken = default);
}
