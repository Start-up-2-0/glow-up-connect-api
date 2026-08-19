using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Negocios;

namespace GLOWAPI.Application.Validators;

public static class ServicoSelecaoValidador
{
    public static void ValidarCombinacao(IReadOnlyList<Servico> servicos)
    {
        if (servicos.Count == 0)
        {
            throw new AgendamentoServicosInvalidosException("Informe ao menos um servico.");
        }

        var combos = servicos.Where(servico => servico.TipoServico == TipoServico.Combo).ToList();
        if (combos.Count > 1)
        {
            throw new AgendamentoServicosInvalidosException("Selecione apenas um combo por agendamento.");
        }

        if (combos.Count == 1 && servicos.Count > 1)
        {
            throw new AgendamentoServicosInvalidosException(
                "Combo nao pode ser combinado com outros servicos.");
        }
    }

    public static void ValidarCombinacaoPorIds(int[] servicoIds, IReadOnlyList<Servico> servicos)
    {
        if (servicoIds.Length == 0)
        {
            throw new AgendamentoServicosInvalidosException("Informe ao menos um servico.");
        }

        var idsDistintos = servicoIds.Distinct().ToArray();
        if (idsDistintos.Length != servicoIds.Length)
        {
            throw new AgendamentoServicosInvalidosException("Servicos duplicados na selecao.");
        }

        if (servicos.Count != idsDistintos.Length)
        {
            throw new AgendamentoServicosInvalidosException(
                "Um ou mais servicos selecionados nao estao disponiveis.");
        }

        ValidarCombinacao(servicos);
    }
}
