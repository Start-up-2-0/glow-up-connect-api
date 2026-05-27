using GLOWAPI.Application.DTOs.Assinaturas;

namespace GLOWAPI.Application.Interfaces.Services;

public interface IAssinaturaService
{
    Task<AssinaturaResponseDto> IniciarAsync(
        IniciarAssinaturaRequestDto request,
        CancellationToken cancellationToken = default);

    Task<AssinaturaResponseDto> TrocarPlanoAsync(
        int assinaturaId,
        TrocarPlanoAssinaturaRequestDto request,
        CancellationToken cancellationToken = default);

    Task<AssinaturaResponseDto> CancelarAsync(
        int assinaturaId,
        CancellationToken cancellationToken = default);
}
