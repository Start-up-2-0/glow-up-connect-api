using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.Interfaces.Services;

public interface IAssinaturaOnboardingFinalizacaoService
{
    Task FinalizarSePendenteAsync(Assinatura assinatura, CancellationToken cancellationToken = default);
}
