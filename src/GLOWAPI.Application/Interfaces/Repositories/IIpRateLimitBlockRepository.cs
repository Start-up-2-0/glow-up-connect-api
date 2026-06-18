using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.Interfaces.Repositories;

public interface IIpRateLimitBlockRepository : IRepository<IpRateLimitBlock>
{
    Task<IpRateLimitBlock?> ObterBloqueioAtivoAsync(string ip, CancellationToken cancellationToken = default);

    Task RegistrarBloqueioAsync(IpRateLimitBlock bloqueio, CancellationToken cancellationToken = default);
}
