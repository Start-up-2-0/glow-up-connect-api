using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Models.Caixa;
using GLOWAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GLOWAPI.Infrastructure.Repositories;

public class LancamentoCaixaRepository : Repository<LancamentoCaixa>, ILancamentoCaixaRepository
{
    public LancamentoCaixaRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<LancamentoCaixa>> ListarPorCaixaAsync(
        LancamentoCaixaFiltro filtro,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .AsNoTracking()
            .Where(lancamento => lancamento.CaixaId == filtro.CaixaId);

        if (filtro.Inicio.HasValue)
        {
            query = query.Where(lancamento => lancamento.CreateAd >= filtro.Inicio.Value);
        }

        if (filtro.Fim.HasValue)
        {
            query = query.Where(lancamento => lancamento.CreateAd < filtro.Fim.Value);
        }

        return await query
            .OrderByDescending(lancamento => lancamento.CreateAd)
            .ThenByDescending(lancamento => lancamento.Id)
            .ToListAsync(cancellationToken);
    }
}
