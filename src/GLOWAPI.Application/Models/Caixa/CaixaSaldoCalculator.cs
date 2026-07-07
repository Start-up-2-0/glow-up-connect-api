using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Application.Models.Caixa;

public record RegistrarLancamentoCaixaComando(
    LancamentoCaixaTipo Tipo,
    decimal Valor,
    string Descricao,
    int? AgendamentoId = null,
    int? PagamentoId = null,
    int? ProfissionalId = null,
    int? LancamentoOriginalId = null,
    int? SessaoCaixaId = null);

public record CaixaSaldosCalculados(
    decimal SaldoTotal,
    decimal SaldoDisponivel,
    decimal SaldoRetido);

public static class CaixaSaldoCalculator
{
    public static CaixaSaldosCalculados Calcular(IReadOnlyList<LancamentoCaixa> lancamentos)
    {
        var lancamentosPorId = lancamentos.ToDictionary(l => l.Id);
        decimal saldo = 0;

        foreach (var lancamento in lancamentos.OrderBy(l => l.CreateAd).ThenBy(l => l.Id))
        {
            LancamentoCaixaTipo? tipoOriginal = null;
            if (lancamento.LancamentoOriginalId.HasValue
                && lancamentosPorId.TryGetValue(lancamento.LancamentoOriginalId.Value, out var original))
            {
                tipoOriginal = original.Tipo;
            }

            saldo += LancamentoCaixaClassificador.ObterImpactoSaldo(
                lancamento.Tipo,
                lancamento.Valor,
                tipoOriginal);
        }

        return new CaixaSaldosCalculados(saldo, saldo, 0);
    }
}
