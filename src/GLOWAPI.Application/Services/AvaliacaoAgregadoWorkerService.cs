using GLOWAPI.Application.Interfaces.Services;

namespace GLOWAPI.Application.Services;

public class AvaliacaoAgregadoWorkerService : IAvaliacaoAgregadoWorkerService
{
    private readonly IAvaliacaoResumoService _avaliacaoResumoService;

    public AvaliacaoAgregadoWorkerService(IAvaliacaoResumoService avaliacaoResumoService)
    {
        _avaliacaoResumoService = avaliacaoResumoService;
    }

    public Task ProcessarCicloDiarioAsync(CancellationToken cancellationToken = default) =>
        _avaliacaoResumoService.RecalcularCachesDiarioAsync(cancellationToken);
}
