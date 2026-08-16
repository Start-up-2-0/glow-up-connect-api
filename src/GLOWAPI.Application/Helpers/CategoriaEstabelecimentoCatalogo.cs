using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Application.Helpers;

/// <summary>
/// Resolve a categoria válida para o tipo de operação (loja vs autônomo).
/// </summary>
public static class CategoriaEstabelecimentoCatalogo
{
    public static int ResolverId(
        int? informado,
        TipoAssinatura tipoAssinatura,
        IReadOnlyList<CategoriaEstabelecimento> categorias,
        Func<string, Exception> criarExcecao)
    {
        var doTipo = categorias
            .Where(categoria => categoria.Ativo && categoria.TipoAssinatura == tipoAssinatura)
            .ToList();

        if (informado is int id)
        {
            if (doTipo.Any(categoria => categoria.Id == id))
            {
                return id;
            }

            throw criarExcecao("Categoria invalida para este tipo de assinatura.");
        }

        if (doTipo.Count == 1)
        {
            return doTipo[0].Id;
        }

        throw criarExcecao(
            tipoAssinatura == TipoAssinatura.ProfissionalAutonomo
                ? "Area de atuacao do profissional e obrigatoria."
                : "Categoria do estabelecimento e obrigatoria.");
    }
}
