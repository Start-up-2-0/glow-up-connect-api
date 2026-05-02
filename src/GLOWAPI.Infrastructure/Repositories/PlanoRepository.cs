using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GLOWAPI.Infrastructure.Repositories;

public class PlanoRepository : Repository<Plano>, IPlanoRepository
{
    public PlanoRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Plano>> ListarAtivosAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(plano => plano.Ativo)
            .OrderBy(plano => plano.Preco)
            .ToListAsync(cancellationToken);
    }
}
