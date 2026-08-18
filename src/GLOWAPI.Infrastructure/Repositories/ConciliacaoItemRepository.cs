using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GLOWAPI.Infrastructure.Repositories;

public class ConciliacaoItemRepository : Repository<ConciliacaoItem>, IConciliacaoItemRepository
{
    public ConciliacaoItemRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<ConciliacaoItem>> ListarPorEstabelecimentoAsync(
        int estabelecimentoId,
        bool? conciliado,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking()
            .Where(item => item.EstabelecimentoId == estabelecimentoId);

        if (conciliado.HasValue)
        {
            query = query.Where(item => item.Conciliado == conciliado.Value);
        }

        return await query
            .OrderByDescending(item => item.DataExtrato)
            .ToListAsync(cancellationToken);
    }
}
