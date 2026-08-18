using GLOWAPI.Application.DTOs.Dashboard;

namespace GLOWAPI.Application.Interfaces.Services;

public interface IDashboardClienteService
{
    Task<DashboardClienteResponseDto> ObterAsync(CancellationToken cancellationToken = default);
}
