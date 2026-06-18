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
             UPDATE `CampanhasPromocionais` AS cp
             SET cp.`Utilizados` = cp.`Utilizados` + 1,
                 cp.`UpdatedAt` = UTC_TIMESTAMP()
             WHERE cp.`Codigo` = {codigo}
               AND cp.`Ativa` = TRUE
               AND (
                 SELECT COUNT(*)
                 FROM `Assinaturas` AS a
                 WHERE a.`CampanhaPromocionalId` = cp.`Id`
               ) < cp.`Limite`
             """);

        return rows > 0;
    }
}
