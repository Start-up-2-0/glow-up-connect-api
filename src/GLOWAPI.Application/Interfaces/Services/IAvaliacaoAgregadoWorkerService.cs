namespace GLOWAPI.Application.Interfaces.Services;

public interface IAvaliacaoAgregadoWorkerService
{
    Task ProcessarCicloDiarioAsync(CancellationToken cancellationToken = default);
}
