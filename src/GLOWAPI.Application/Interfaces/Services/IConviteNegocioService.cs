using GLOWAPI.Application.DTOs.Convites;

namespace GLOWAPI.Application.Interfaces.Services;

public interface IConviteNegocioService
{
    Task<ConviteNegocioResponseDto> CriarConviteProfissionalAsync(
        int estabelecimentoId,
        CriarConviteProfissionalRequestDto request,
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
