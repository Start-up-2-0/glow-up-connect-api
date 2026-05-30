using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GLOWAPI.Infrastructure.Repositories;

public class CaixaRepository : Repository<Caixa>, ICaixaRepository
{
    public CaixaRepository(ApplicationDbContext context) : base(context)
    {
    }

    public Task<Caixa?> ObterPorEstabelecimentoAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken = default)
    {
        return DbSet.AsNoTracking().FirstOrDefaultAsync(
            caixa => caixa.EstabelecimentoId == estabelecimentoId,
            cancellationToken);
    }
}
