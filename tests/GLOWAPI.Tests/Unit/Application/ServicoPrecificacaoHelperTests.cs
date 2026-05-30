using GLOWAPI.Application.DTOs.Equipe;
using GLOWAPI.Application.Helpers;
using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Tests.Unit.Application;

public class ServicoPrecificacaoHelperTests
{
    private static readonly Servico ServicoBase = new()
    {
        Id = 1,
        PrecoBase = 80,
        DuracaoMinutos = 45
    };

    [Fact]
    public void ObterDuracaoEfetiva_DeveUsarVinculoAtivo()
    {
        var vinculo = new ProfissionalServico
        {
            DuracaoMinutos = 60,
            Ativo = true
        };

        Assert.Equal(60, ServicoPrecificacaoHelper.ObterDuracaoEfetiva(ServicoBase, vinculo));
    }

    [Fact]
    public void ObterDuracaoEfetiva_DeveUsarServico_QuandoVinculoInativo()
    {
        var vinculo = new ProfissionalServico
        {
            DuracaoMinutos = 60,
            Ativo = false
        };

        Assert.Equal(45, ServicoPrecificacaoHelper.ObterDuracaoEfetiva(ServicoBase, vinculo));
    }

    [Fact]
    public void ObterPrecoEfetivo_DeveUsarVinculoAtivo()
    {
        var vinculo = new ProfissionalServicoResponseDto
        {
            Preco = 95,
            Ativo = true
        };

        Assert.Equal(95, ServicoPrecificacaoHelper.ObterPrecoEfetivo(ServicoBase, vinculo));
    }

    [Fact]
    public void ObterPrecoEfetivo_DeveUsarPrecoBase_QuandoVinculoNulo()
    {
        Assert.Equal(80, ServicoPrecificacaoHelper.ObterPrecoEfetivo(ServicoBase, (ProfissionalServico?)null));
    }
}
