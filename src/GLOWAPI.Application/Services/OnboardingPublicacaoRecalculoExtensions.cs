using GLOWAPI.Application.Interfaces.Services;

namespace GLOWAPI.Application.Services;

internal static class OnboardingPublicacaoRecalculoExtensions
{
    public static Task RecalcularPublicacaoAsync(
        this IOnboardingPublicacaoService onboardingPublicacaoService,
        int estabelecimentoId,
        CancellationToken cancellationToken = default) =>
        onboardingPublicacaoService.RecalcularVisibilidadeAsync(estabelecimentoId, cancellationToken);
}
