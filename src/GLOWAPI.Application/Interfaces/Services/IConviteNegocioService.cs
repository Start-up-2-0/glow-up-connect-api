using GLOWAPI.Application.DTOs.Convites;

namespace GLOWAPI.Application.Interfaces.Services;

public interface IConviteNegocioService
{
    Task<ConviteNegocioCriadoResponseDto> CriarConviteProfissionalAsync(
        int estabelecimentoId,
        CriarConviteProfissionalRequestDto request,
        CancellationToken cancellationToken = default);

    Task<ConviteNegocioCriadoResponseDto> CriarConviteUsuarioEquipeAsync(
        int estabelecimentoId,
        CriarConviteUsuarioEquipeRequestDto request,
        CancellationToken cancellationToken = default);

    Task<ConviteNegocioPreviewResponseDto> ObterPreviewAsync(
        string token,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ConviteNegocioResponseDto>> ListarAsync(
        int estabelecimentoId,
        ConviteNegocioFiltroDto filtro,
        CancellationToken cancellationToken = default);

    Task<ConviteNegocioResponseDto> AceitarAsync(
        string token,
        CancellationToken cancellationToken = default);

    Task<ConviteNegocioResponseDto> RejeitarAsync(
        string token,
        CancellationToken cancellationToken = default);

    Task<ConviteNegocioResponseDto> CancelarAsync(
        int estabelecimentoId,
        int conviteId,
        CancellationToken cancellationToken = default);
}
