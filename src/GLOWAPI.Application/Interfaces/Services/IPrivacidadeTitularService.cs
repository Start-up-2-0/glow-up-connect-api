namespace GLOWAPI.Application.Interfaces.Services;

public interface IPrivacidadeTitularService
{
    Task<object> ExportarMeusDadosAsync(CancellationToken cancellationToken = default);
    Task SolicitarExclusaoAsync(CancellationToken cancellationToken = default);
    Task RevogarConsentimentoAsync(CancellationToken cancellationToken = default);
}
