using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GLOWAPI.Infrastructure.Repositories;

public class MetaRepository : Repository<Meta>, IMetaRepository
{
    public MetaRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Meta>> ListarAtivasPorEstabelecimentoAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(m => m.EstabelecimentoId == estabelecimentoId && m.Ativa)
            .OrderBy(m => m.Nome)
            .ToListAsync(cancellationToken);
    }

    public Task<Meta?> ObterPorIdComTrackingAsync(
        int metaId,
        CancellationToken cancellationToken = default)
    {
        return DbSet.FirstOrDefaultAsync(m => m.Id == metaId, cancellationToken);
    }

    public Task<bool> ExisteMetaAtivaNoPeriodoAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken = default)
    {
        return DbSet.AnyAsync(m => m.EstabelecimentoId == estabelecimentoId && m.Ativa, cancellationToken);
    }
}
