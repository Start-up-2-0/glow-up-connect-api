namespace GLOWAPI.Application.Interfaces.Services;

public interface IAssinaturaCobrancaWorkerService
{
    Task ProcessarCicloDiarioAsync(CancellationToken cancellationToken = default);
}
