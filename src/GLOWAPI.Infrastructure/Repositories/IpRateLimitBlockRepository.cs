using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GLOWAPI.Infrastructure.Repositories;

public class IpRateLimitBlockRepository : Repository<IpRateLimitBlock>, IIpRateLimitBlockRepository
{
    public IpRateLimitBlockRepository(ApplicationDbContext context) : base(context)
    {
    }

    public Task<IpRateLimitBlock?> ObterBloqueioAtivoAsync(string ip, CancellationToken cancellationToken = default)
    {
        var agora = DateTime.UtcNow;
        return Context.IpRateLimitBlocks
            .Where(b => b.Ip == ip && b.BlockedUntil > agora)
            .OrderByDescending(b => b.BlockedUntil)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task RegistrarBloqueioAsync(IpRateLimitBlock bloqueio, CancellationToken cancellationToken = default)
    {
        await AdicionarAsync(bloqueio, cancellationToken);
        await SalvarAlteracoesAsync(cancellationToken);
    }
}
