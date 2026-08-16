using GLOWAPI.Application.DTOs.Convites;
using GLOWAPI.Application.DTOs.Usuario;

namespace GLOWAPI.Application.Interfaces.Services;

public interface IConviteNegocioService
{
    Task<ConviteNegocioCriadoResponseDto> CriarLinkAsync(
        int estabelecimentoId,
        CriarConviteLinkRequestDto request,
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

    Task<ConviteNegocioResponseDto> AceitarComCadastroAsync(
        string token,
        CadastrarClienteDto cadastro,
        CancellationToken cancellationToken = default);

    Task<ConviteNegocioResponseDto> CancelarAsync(
        int estabelecimentoId,
        int conviteId,
        CancellationToken cancellationToken = default);
}
