using GLOWAPI.Domain.Entities;
using GLOWAPI.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GLOWAPI.Tests.Helpers;

public static class CampanhaPromocionalTestHelper
{
    public const string CodigoLancamento = "lancamento-100";

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
