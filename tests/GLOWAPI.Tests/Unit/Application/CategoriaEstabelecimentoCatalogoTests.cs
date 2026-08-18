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
            Nome = "Barbeiro ou cabeleireiro(a)",
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
        var idAutonomo = CategoriaEstabelecimentoCatalogo.ResolverId(
            null,
            TipoAssinatura.ProfissionalAutonomo,
            Catalogo,
            mensagem => new InvalidOperationException(mensagem));

        Assert.Equal(1, idLoja);
        Assert.Equal(2, idAutonomo);
    }
}
