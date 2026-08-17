using GLOWAPI.Application.Models.Auth;

namespace GLOWAPI.Application.Interfaces.Services;

public interface IExclusaoContaService
{
    Task SolicitarAsync(string senha, CancellationToken cancellationToken = default);

    Task<AuthLoginResult> ReativarAsync(
        string email,
        string senha,
        AuthSessionContext context,
        CancellationToken cancellationToken = default);

    Task<int> EfetivarVencidasAsync(CancellationToken cancellationToken = default);
}
