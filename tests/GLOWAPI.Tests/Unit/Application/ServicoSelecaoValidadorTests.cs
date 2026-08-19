using GLOWAPI.Application.Validators;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Negocios;

namespace GLOWAPI.Tests.Unit.Application;

public class ServicoSelecaoValidadorTests
{
    [Fact]
    public void ValidarCombinacao_DevePermitirMultiplosIndividuais()
    {
        var servicos = new List<Servico>
        {
            CriarServico(1, TipoServico.Individual),
            CriarServico(2, TipoServico.Individual),
            CriarServico(3, TipoServico.Individual),
        };

        ServicoSelecaoValidador.ValidarCombinacao(servicos);
    }

    [Fact]
    public void ValidarCombinacao_DevePermitirComboSozinho()
    {
        ServicoSelecaoValidador.ValidarCombinacao([CriarServico(10, TipoServico.Combo)]);
    }

    [Fact]
    public void ValidarCombinacao_DeveRejeitarComboComIndividuais()
    {
        var servicos = new List<Servico>
        {
            CriarServico(10, TipoServico.Combo),
            CriarServico(1, TipoServico.Individual),
        };

        Assert.Throws<AgendamentoServicosInvalidosException>(() =>
            ServicoSelecaoValidador.ValidarCombinacao(servicos));
    }

    [Fact]
    public void ValidarCombinacao_DeveRejeitarDoisCombos()
    {
        var servicos = new List<Servico>
        {
            CriarServico(10, TipoServico.Combo),
            CriarServico(11, TipoServico.Combo),
        };

        Assert.Throws<AgendamentoServicosInvalidosException>(() =>
            ServicoSelecaoValidador.ValidarCombinacao(servicos));
    }

    [Fact]
    public void ValidarCombinacaoPorIds_DeveRejeitarIdsDuplicados()
    {
        var servicos = new List<Servico> { CriarServico(1, TipoServico.Individual) };

        Assert.Throws<AgendamentoServicosInvalidosException>(() =>
            ServicoSelecaoValidador.ValidarCombinacaoPorIds([1, 1], servicos));
    }

    private static Servico CriarServico(int id, TipoServico tipo) =>
        new()
        {
            Id = id,
            Nome = $"Servico {id}",
            TipoServico = tipo,
            Ativo = true,
        };
}
