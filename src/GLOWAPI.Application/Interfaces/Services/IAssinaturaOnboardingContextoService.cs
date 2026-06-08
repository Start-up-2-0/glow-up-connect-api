using GLOWAPI.Application.DTOs.Assinaturas;

namespace GLOWAPI.Application.Interfaces.Services;

public interface IAssinaturaOnboardingContextoService
{
    Task<AssinaturaOnboardingContextoResponseDto> ObterContextoAsync(
        CancellationToken cancellationToken = default);
}
