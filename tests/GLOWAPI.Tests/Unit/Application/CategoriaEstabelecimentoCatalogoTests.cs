using GLOWAPI.Application.Helpers;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Tests.Unit.Application;

public class CategoriaEstabelecimentoCatalogoTests
{
    private static readonly IReadOnlyList<CategoriaEstabelecimento> Catalogo =
    [
        new()
        {
            Id = 1,
            Nome = "Barbearia ou salão de beleza",
            TipoAssinatura = TipoAssinatura.Estabelecimento,
            Ativo = true
        },
        new()
        {
            Id = 2,
            Nome = "Barbeiro",
            TipoAssinatura = TipoAssinatura.ProfissionalAutonomo,
            Ativo = true
        },
        new()
        {
            Id = 3,
            Nome = "Cabeleireiro(a)",
            TipoAssinatura = TipoAssinatura.ProfissionalAutonomo,
            Ativo = true
        }
    ];

    [Fact]
    public void ResolverId_DeveRetornarCategoriaInformada_QuandoPertenceAoTipo()
    {
        var id = CategoriaEstabelecimentoCatalogo.ResolverId(
            1,
            TipoAssinatura.Estabelecimento,
            Catalogo,
            mensagem => new InvalidOperationException(mensagem));

        Assert.Equal(1, id);
    }

    [Fact]
    public void ResolverId_DeveLancarExcecao_QuandoCategoriaNaoPertenceAoTipo()
    {
        var excecao = Assert.Throws<InvalidOperationException>(() =>
            CategoriaEstabelecimentoCatalogo.ResolverId(
                1,
                TipoAssinatura.ProfissionalAutonomo,
                Catalogo,
                mensagem => new InvalidOperationException(mensagem)));

        Assert.Equal("Categoria invalida para este tipo de assinatura.", excecao.Message);
    }

    [Fact]
    public void ResolverId_DeveAtribuirCategoriaUnicaDoTipo_QuandoNaoInformada()
    {
        var idLoja = CategoriaEstabelecimentoCatalogo.ResolverId(
            null,
            TipoAssinatura.Estabelecimento,
            Catalogo,
            mensagem => new InvalidOperationException(mensagem));

        Assert.Equal(1, idLoja);
    }

    [Fact]
    public void ResolverId_DeveExigirEscolha_QuandoAutonomoTemVariasCategorias()
    {
        var excecao = Assert.Throws<InvalidOperationException>(() =>
            CategoriaEstabelecimentoCatalogo.ResolverId(
                null,
                TipoAssinatura.ProfissionalAutonomo,
                Catalogo,
                mensagem => new InvalidOperationException(mensagem)));

        Assert.Equal("Area de atuacao do profissional e obrigatoria.", excecao.Message);
    }
}
