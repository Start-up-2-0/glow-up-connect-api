using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Application.Models.Caixa;

public static class LancamentoCaixaClassificador
{
    private static readonly HashSet<LancamentoCaixaTipo> TiposEntrada =
    [
        LancamentoCaixaTipo.EntradaAgendamento,
        LancamentoCaixaTipo.AjusteManual
    ];

    private static readonly HashSet<LancamentoCaixaTipo> TiposSaida =
    [
        LancamentoCaixaTipo.ComissaoProfissional,
        LancamentoCaixaTipo.TaxaPlataforma,
        LancamentoCaixaTipo.Assinatura,
        LancamentoCaixaTipo.Saque,
        LancamentoCaixaTipo.Estorno,
        LancamentoCaixaTipo.Chargeback
    ];

    public static bool EhEntrada(LancamentoCaixaTipo tipo) => TiposEntrada.Contains(tipo);

    public static bool EhSaida(LancamentoCaixaTipo tipo) => TiposSaida.Contains(tipo);

    public static decimal ObterImpactoSaldo(
        LancamentoCaixaTipo tipo,
        decimal valor,
        LancamentoCaixaTipo? tipoOriginal = null)
    {
        if (tipo == LancamentoCaixaTipo.Estorno && tipoOriginal.HasValue)
        {
            return -ObterImpactoSaldo(tipoOriginal.Value, valor);
        }

        if (EhEntrada(tipo))
        {
            return valor;
        }

        if (EhSaida(tipo))
        {
            return -valor;
        }

        return 0;
    }

    public static LancamentoCaixaTipo ResolverTipoAjuste(SubtipoAjusteManual subtipo) =>
        subtipo switch
        {
            SubtipoAjusteManual.Reforco => LancamentoCaixaTipo.AjusteManual,
            SubtipoAjusteManual.Sangria => LancamentoCaixaTipo.Saque,
            _ => throw new ArgumentOutOfRangeException(nameof(subtipo))
        };
}
