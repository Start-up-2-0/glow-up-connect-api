using GLOWAPI.Domain.Entities;
using GLOWAPI.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GLOWAPI.Tests.Helpers;

public static class CampanhaPromocionalTestHelper
{
    public const string CodigoLancamento = "lancamento-100";

    public static async Task HabilitarPromocaoAsync(
        ApplicationDbContext db,
        int utilizados = 0,
        CancellationToken cancellationToken = default)
    {
        var campanha = await db.CampanhasPromocionais
            .FirstOrDefaultAsync(item => item.Codigo == CodigoLancamento, cancellationToken);

        if (campanha is null)
        {
            campanha = new CampanhaPromocional
            {
                Codigo = CodigoLancamento,
                Limite = 100,
                Utilizados = utilizados,
                DiasTrial = 30,
                Ativa = true
            };
            db.CampanhasPromocionais.Add(campanha);
        }
        else
        {
            campanha.Limite = 100;
            campanha.Utilizados = utilizados;
            campanha.DiasTrial = 30;
            campanha.Ativa = true;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public static async Task DesabilitarPromocaoAsync(ApplicationDbContext db, CancellationToken cancellationToken = default)
    {
        var campanha = await db.CampanhasPromocionais
            .FirstOrDefaultAsync(item => item.Codigo == CodigoLancamento, cancellationToken);

        if (campanha is null)
        {
            campanha = new CampanhaPromocional
            {
                Codigo = CodigoLancamento,
                Limite = 100,
                Utilizados = 100,
                DiasTrial = 30,
                Ativa = true
            };
            db.CampanhasPromocionais.Add(campanha);
        }
        else
        {
            campanha.Utilizados = campanha.Limite;
        }
    }
}
