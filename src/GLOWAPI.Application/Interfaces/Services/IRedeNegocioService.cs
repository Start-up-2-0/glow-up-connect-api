using GLOWAPI.Application.DTOs.Rede;

namespace GLOWAPI.Application.Interfaces.Services;

public interface IRedeNegocioService
{
    Task<RedeResumoResponseDto> ObterResumoAsync(
        int assinaturaId,
        int usuarioId,
        DateTime? inicio,
        DateTime? fim,
        CancellationToken cancellationToken = default);
}
