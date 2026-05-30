using GLOWAPI.Application.Validators;
using GLOWAPI.Domain.Exceptions.Negocios;

namespace GLOWAPI.Tests.Unit.Application;

public class ServicoValidadorTests
{
    [Fact]
    public void ValidarServico_DeveAceitarDadosValidos()
    {
        ServicoValidador.ValidarServico("Corte", "Descricao", 50, 45);
    }

    [Fact]
    public void ValidarServico_DeveAceitarDescricaoOpcional()
    {
        ServicoValidador.ValidarServico("Corte", null, 0, 1);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidarServico_DeveLancarExcecao_QuandoNomeInvalido(string nome)
    {
        Assert.Throws<ServicoNegocioInvalidoException>(() =>
            ServicoValidador.ValidarServico(nome, null, 10, 30));
    }

    [Fact]
    public void ValidarServico_DeveLancarExcecao_QuandoPrecoNegativo()
    {
        Assert.Throws<ServicoNegocioInvalidoException>(() =>
            ServicoValidador.ValidarServico("Corte", null, -1, 30));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(481)]
    public void ValidarServico_DeveLancarExcecao_QuandoDuracaoInvalida(int duracao)
    {
        Assert.Throws<ServicoNegocioInvalidoException>(() =>
            ServicoValidador.ValidarServico("Corte", null, 10, duracao));
    }
}
