using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GLOWAPI.Infrastructure.Repositories;

public class CampanhaPromocionalRepository : Repository<CampanhaPromocional>, ICampanhaPromocionalRepository
{
    public CampanhaPromocionalRepository(ApplicationDbContext context) : base(context)
    {
    }

    public Task<CampanhaPromocional?> ObterAtivaPorCodigoAsync(
        string codigo,
        CancellationToken cancellationToken = default) =>
        DbSet.AsNoTracking()
            .FirstOrDefaultAsync(
                campanha => campanha.Codigo == codigo && campanha.Ativa,
                cancellationToken);

    public async Task<bool> TentarReservarVagaAsync(
        string codigo,
        CancellationToken cancellationToken = default)
    {
        var rows = await Context.Database.ExecuteSqlInterpolatedAsync(
            $"""
             UPDATE `CampanhasPromocionais`
             SET `Utilizados` = `Utilizados` + 1,
                 `UpdatedAt` = UTC_TIMESTAMP()
             WHERE `Codigo` = {codigo}
               AND `Ativa` = TRUE
               AND `Utilizados` < `Limite`
             """);

        return rows > 0;
    }
}
