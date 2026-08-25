using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GLOWAPI.Infrastructure.Repositories;

public class ComodidadeRepository : Repository<Comodidade>, IComodidadeRepository
{
    public ComodidadeRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Comodidade>> ListarAtivasAsync(CancellationToken cancellationToken = default) =>
        await DbSet.AsNoTracking()
            .Where(comodidade => comodidade.Ativo)
            .OrderBy(comodidade => comodidade.Ordem)
            .ThenBy(comodidade => comodidade.Nome)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Comodidade>> ListarPorEstabelecimentoAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken = default) =>
        await Context.EstabelecimentoComodidades.AsNoTracking()
            .Where(vinculo => vinculo.EstabelecimentoId == estabelecimentoId
                && vinculo.Comodidade != null
                && vinculo.Comodidade.Ativo)
            .Select(vinculo => vinculo.Comodidade!)
            .OrderBy(comodidade => comodidade.Ordem)
            .ThenBy(comodidade => comodidade.Nome)
            .ToListAsync(cancellationToken);

    public async Task SubstituirDoEstabelecimentoAsync(
        int estabelecimentoId,
        IReadOnlyCollection<int> comodidadeIds,
        CancellationToken cancellationToken = default)
    {
        var atuais = await Context.EstabelecimentoComodidades
            .Where(vinculo => vinculo.EstabelecimentoId == estabelecimentoId)
            .ToListAsync(cancellationToken);
        Context.EstabelecimentoComodidades.RemoveRange(atuais);

        await Context.EstabelecimentoComodidades.AddRangeAsync(
            comodidadeIds.Distinct().Select(comodidadeId => new EstabelecimentoComodidade
            {
                EstabelecimentoId = estabelecimentoId,
                ComodidadeId = comodidadeId
            }),
            cancellationToken);

        await Context.SaveChangesAsync(cancellationToken);
    }
}
