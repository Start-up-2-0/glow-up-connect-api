namespace GLOWAPI.Application.Interfaces.Services;

public interface ILoginFailureRateLimitService
{
    Task<bool> PodeTentarAsync(string ip, CancellationToken cancellationToken = default);

    Task RegistrarFalhaAsync(string ip, CancellationToken cancellationToken = default);
}
