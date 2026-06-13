using GLOWAPI.Application.DTOs.Auditoria;

namespace GLOWAPI.Application.Interfaces.Services;

public interface IAuditoriaConsultaNegocioService
{
    Task<IReadOnlyList<AuditoriaNegocioResponseDto>> ListarRecentesAsync(
        int estabelecimentoId,
        int limite,
        CancellationToken cancellationToken = default);
}
