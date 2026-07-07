using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GLOWAPI.Infrastructure.Repositories;

public class ContaReceberRepository : Repository<ContaReceber>, IContaReceberRepository
{
    public ContaReceberRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<ContaReceber>> ListarPorEstabelecimentoAsync(
        int estabelecimentoId,
        ContaFinanceiraStatus? status,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking()
            .Where(conta => conta.EstabelecimentoId == estabelecimentoId);

        if (status.HasValue)
        {
            query = query.Where(conta => conta.Status == status.Value);
        }

        return await query
            .OrderBy(conta => conta.Vencimento)
            .ToListAsync(cancellationToken);
    }

    public Task<ContaReceber?> ObterPorIdEEstabelecimentoAsync(
        int contaId,
        int estabelecimentoId,
        CancellationToken cancellationToken = default)
    {
        return DbSet.FirstOrDefaultAsync(
            conta => conta.Id == contaId && conta.EstabelecimentoId == estabelecimentoId,
            cancellationToken);
    }
}
