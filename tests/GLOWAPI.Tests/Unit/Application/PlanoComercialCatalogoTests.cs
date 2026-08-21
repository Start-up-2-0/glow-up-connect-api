using GLOWAPI.Application.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Tests.Unit.Application;

public class PlanoComercialCatalogoTests
{
    [Fact]
    public void Obter_EstabelecimentoEssencial_DeveIncluirProfissionaisEWhatsApp()
    {
        var perfil = PlanoComercialCatalogo.Obter(
            new Plano { Nome = "Essencial", LimiteEstabelecimentos = 1 },
            TipoAssinatura.Estabelecimento);

        Assert.Contains(ModuloAssinatura.Profissionais, perfil.Modulos);
        Assert.Contains(ModuloAssinatura.WhatsApp, perfil.Modulos);
        Assert.DoesNotContain(ModuloAssinatura.Clientes, perfil.Modulos);
        Assert.DoesNotContain(ModuloAssinatura.Caixa, perfil.Modulos);
        Assert.Equal(1, perfil.LimiteEstabelecimentosEfetivo);
    }

    [Fact]
    public void Obter_EstabelecimentoPremium_DeveIncluirFinanceiroComissaoEClientes()
    {
        var perfil = PlanoComercialCatalogo.Obter(
            new Plano { Nome = "Premium", LimiteEstabelecimentos = 5 },
            TipoAssinatura.Estabelecimento);

        Assert.Contains(ModuloAssinatura.Caixa, perfil.Modulos);
        Assert.Contains(ModuloAssinatura.Financeiro, perfil.Modulos);
        Assert.Contains(ModuloAssinatura.ComissaoProfissionais, perfil.Modulos);
        Assert.Contains(ModuloAssinatura.Clientes, perfil.Modulos);
        Assert.True(perfil.PrioridadeListagemPublica);
        Assert.Equal(5, perfil.LimiteEstabelecimentosEfetivo);
        Assert.True(PlanoComercialCatalogo.PermiteMultiLoja(
            new Plano { Nome = "Premium", LimiteEstabelecimentos = 5 },
            TipoAssinatura.Estabelecimento));
    }

    [Fact]
    public void Obter_AutonomoEssencial_DeveIncluirClientesSemEquipeNemWhatsApp()
    {
        var perfil = PlanoComercialCatalogo.Obter(
            new Plano { Nome = "Essencial", LimiteEstabelecimentos = 1 },
            TipoAssinatura.ProfissionalAutonomo);

        Assert.Contains(ModuloAssinatura.Agenda, perfil.Modulos);
        Assert.Contains(ModuloAssinatura.Clientes, perfil.Modulos);
        Assert.Contains(ModuloAssinatura.ProfissionalAutonomo, perfil.Modulos);
        Assert.DoesNotContain(ModuloAssinatura.Profissionais, perfil.Modulos);
        Assert.DoesNotContain(ModuloAssinatura.WhatsApp, perfil.Modulos);
        Assert.DoesNotContain(ModuloAssinatura.ComissaoProfissionais, perfil.Modulos);
        Assert.Equal(1, perfil.LimiteUsuarios);
        Assert.Equal(1, perfil.LimiteProfissionaisEfetivo);
        Assert.Equal(1, perfil.LimiteEstabelecimentosEfetivo);
        Assert.False(PlanoComercialCatalogo.PermiteMultiLoja(
            new Plano { Nome = "Premium", LimiteEstabelecimentos = 5 },
            TipoAssinatura.ProfissionalAutonomo));
    }

    [Fact]
    public void Obter_AutonomoPremium_DeveIncluirWhatsAppEFinanceiroSemComissao()
    {
        var perfil = PlanoComercialCatalogo.Obter(
            new Plano { Nome = "Premium", LimiteEstabelecimentos = 5 },
            TipoAssinatura.ProfissionalAutonomo);

        Assert.Contains(ModuloAssinatura.WhatsApp, perfil.Modulos);
        Assert.Contains(ModuloAssinatura.Caixa, perfil.Modulos);
        Assert.Contains(ModuloAssinatura.Financeiro, perfil.Modulos);
        Assert.Contains(ModuloAssinatura.Clientes, perfil.Modulos);
        Assert.DoesNotContain(ModuloAssinatura.Profissionais, perfil.Modulos);
        Assert.DoesNotContain(ModuloAssinatura.ComissaoProfissionais, perfil.Modulos);
        Assert.True(perfil.PrioridadeListagemPublica);
        Assert.Equal(1, perfil.LimiteEstabelecimentosEfetivo);
    }

    [Fact]
    public void EhModuloExclusivoEstabelecimento_DeveIdentificarEquipeEComissao()
    {
        Assert.True(PlanoComercialCatalogo.EhModuloExclusivoEstabelecimento(ModuloAssinatura.Profissionais));
        Assert.True(PlanoComercialCatalogo.EhModuloExclusivoEstabelecimento(ModuloAssinatura.ComissaoProfissionais));
        Assert.False(PlanoComercialCatalogo.EhModuloExclusivoEstabelecimento(ModuloAssinatura.Agenda));
    }

    [Fact]
    public void ResolverPreco_Autonomo_DeveUsarTabela49_99E79_99()
    {
        Assert.Equal(
            49.99m,
            PlanoComercialCatalogo.ResolverPreco(
                new Plano { Nome = "Essencial", Preco = 79.90m },
                TipoAssinatura.ProfissionalAutonomo));
        Assert.Equal(
            79.99m,
            PlanoComercialCatalogo.ResolverPreco(
                new Plano { Nome = "Premium", Preco = 199.90m },
                TipoAssinatura.ProfissionalAutonomo));
        Assert.Equal(
            79.90m,
            PlanoComercialCatalogo.ResolverPreco(
                new Plano { Nome = "Essencial", Preco = 79.90m },
                TipoAssinatura.Estabelecimento));
    }

    [Fact]
    public void ResolverDescricao_Autonomo_DeveUsarCopySemEquipeNemMultiUnidade()
    {
        var essencial = new Plano
        {
            Nome = "Essencial",
            Descricao = "Operacao completa com equipe, WhatsApp e gestao para uma unidade"
        };
        var premium = new Plano
        {
            Nome = "Premium",
            Descricao = "Caixa, financeiro, comissoes, ate 5 unidades e prioridade no marketplace"
        };

        Assert.Equal(
            "Agenda, clientes e perfil público para quem atende sozinho",
            PlanoComercialCatalogo.ResolverDescricao(essencial, TipoAssinatura.ProfissionalAutonomo));
        Assert.Equal(
            "WhatsApp, caixa pessoal, financeiro e prioridade no marketplace",
            PlanoComercialCatalogo.ResolverDescricao(premium, TipoAssinatura.ProfissionalAutonomo));
        Assert.Equal(
            "Operação completa com equipe, WhatsApp e gestão para uma unidade",
            PlanoComercialCatalogo.ResolverDescricao(essencial, TipoAssinatura.Estabelecimento));
        Assert.Equal(
            "Caixa, financeiro, comissões, até 5 unidades e prioridade no marketplace",
            PlanoComercialCatalogo.ResolverDescricao(premium, TipoAssinatura.Estabelecimento));
    }

    [Fact]
    public void Obter_Funcionalidades_DevemUsarOrtografiaComAcentos()
    {
        var essencialLoja = PlanoComercialCatalogo.Obter(
            new Plano { Nome = "Essencial" },
            TipoAssinatura.Estabelecimento);
        var premiumAutonomo = PlanoComercialCatalogo.Obter(
            new Plano { Nome = "Premium" },
            TipoAssinatura.ProfissionalAutonomo);

        Assert.Contains("Cadastro de serviços", essencialLoja.Funcionalidades);
        Assert.Contains("Configuração de horários", essencialLoja.Funcionalidades);
        Assert.Contains("Confirmação automática via WhatsApp", essencialLoja.Funcionalidades);
        Assert.Contains("Perfil profissional público", premiumAutonomo.Funcionalidades);
        Assert.Contains("Presença no Explorar Lojas", premiumAutonomo.Funcionalidades);
        Assert.Contains("Relatórios financeiros", premiumAutonomo.Funcionalidades);
        Assert.Contains("Dashboard avançado", premiumAutonomo.Funcionalidades);
    }
}
