using GLOWAPI.Application.DTOs.Assinaturas;
using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.Interfaces.Services;

public interface IOnboardingPublicacaoService
{
    Task<OnboardingPublicacaoStatusDto> ObterStatusAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken = default);

    Task<bool> RecalcularVisibilidadeAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken = default);

    Task RecalcularVisibilidadePorAssinaturaAsync(
        Assinatura assinatura,
        CancellationToken cancellationToken = default);
}
