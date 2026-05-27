using GLOWAPI.Application.DTOs.Planos;

namespace GLOWAPI.Application.Interfaces.Services;

public interface IPlanoService
{
    Task<IReadOnlyList<PlanoResponseDto>> ListarAtivosAsync(CancellationToken cancellationToken = default);
}
