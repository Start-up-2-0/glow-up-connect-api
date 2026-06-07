using GLOWAPI.Application.DTOs.Planos;

namespace GLOWAPI.Application.Interfaces.Services;

public interface IPlanoService
{
    Task<PlanosAtivosResponseDto> ListarAtivosAsync(CancellationToken cancellationToken = default);
}
