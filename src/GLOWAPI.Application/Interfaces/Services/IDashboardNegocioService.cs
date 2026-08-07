using GLOWAPI.Application.DTOs.Dashboard;

namespace GLOWAPI.Application.Interfaces.Services;

public interface IDashboardNegocioService
{
    Task<DashboardNegocioResponseDto> ObterAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken = default);
}
