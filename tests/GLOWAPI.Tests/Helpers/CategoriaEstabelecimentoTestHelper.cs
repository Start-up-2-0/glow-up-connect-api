using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GLOWAPI.Tests.Helpers;

public static class CategoriaEstabelecimentoTestHelper
{
    public static async Task GarantirCatalogoAsync(
        ApplicationDbContext db,
        CancellationToken cancellationToken = default)
    {
        await GarantirCategoriaAsync(
            db,
            "Barbearia",
            TipoAssinatura.Estabelecimento,
            cancellationToken);
        await GarantirCategoriaAsync(
            db,
            "Salão de Beleza",
            TipoAssinatura.Estabelecimento,
            cancellationToken);
        await GarantirCategoriaAsync(
            db,
            "Barbeiro",
            TipoAssinatura.ProfissionalAutonomo,
            cancellationToken);
    }

    private static async Task GarantirCategoriaAsync(
        ApplicationDbContext db,
        string nome,
        TipoAssinatura tipoAssinatura,
        CancellationToken cancellationToken)
    {
        var existente = await db.CategoriasEstabelecimento
            .FirstOrDefaultAsync(
                categoria => categoria.TipoAssinatura == tipoAssinatura
                    && categoria.Nome == nome,
                cancellationToken);

        if (existente is null)
        {
            db.CategoriasEstabelecimento.Add(new CategoriaEstabelecimento
            {
                Nome = nome,
                TipoAssinatura = tipoAssinatura,
                Ativo = true
            });
            return;
        }

        existente.Nome = nome;
        existente.Ativo = true;
    }
}
