using System.Reflection;
using GLOWAPI.API.Attributes;
using GLOWAPI.API.Controllers;
using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Tests.Unit.API;

public class EstabelecimentosControllerAcessoTests
{
    [Fact]
    public void AtualizarPerfil_DeveExigirPermissaoEditarNegocio()
    {
        var method = typeof(EstabelecimentosController).GetMethod(nameof(EstabelecimentosController.AtualizarPerfil));

        var atributo = method!.GetCustomAttributes<RequerPermissaoNegocioAttribute>()
            .SingleOrDefault();

        Assert.NotNull(atributo);
        Assert.Equal(PermissaoNegocio.NegocioEditar, atributo!.Permissao);
        Assert.Equal("estabelecimentoId", atributo.ParametroId);
    }

    [Fact]
    public void ListarAgendaGeral_DevePermitirUsoDaPermissaoDaRecepcionista()
    {
        var method = typeof(EstabelecimentosController).GetMethod(nameof(EstabelecimentosController.ListarAgendaGeral));

        var atributo = method!.GetCustomAttributes<RequerPermissaoNegocioAttribute>()
            .SingleOrDefault();

        Assert.NotNull(atributo);
        Assert.Equal(PermissaoNegocio.AgendaVisualizarGeral, atributo!.Permissao);
        Assert.Equal("estabelecimentoId", atributo.ParametroId);
    }

    [Fact]
    public void CadastrarUsuarioEquipe_DeveExigirPermissaoGerenciarEquipe()
    {
        var method = typeof(EstabelecimentosController).GetMethod(nameof(EstabelecimentosController.CadastrarUsuarioEquipe));

        var atributo = method!.GetCustomAttributes<RequerPermissaoNegocioAttribute>()
            .SingleOrDefault();

        Assert.NotNull(atributo);
        Assert.Equal(PermissaoNegocio.EquipeGerenciar, atributo!.Permissao);
        Assert.Equal("estabelecimentoId", atributo.ParametroId);
    }

    [Fact]
    public void AtualizarRoleUsuarioEquipe_DeveExigirPermissaoGerenciarEquipe()
    {
        var method = typeof(EstabelecimentosController).GetMethod(nameof(EstabelecimentosController.AtualizarRoleUsuarioEquipe));

        var permissao = method!.GetCustomAttributes<RequerPermissaoNegocioAttribute>()
            .SingleOrDefault();
        var modulo = method!.GetCustomAttributes<RequerModuloAssinaturaAttribute>()
            .SingleOrDefault();

        Assert.NotNull(permissao);
        Assert.Equal(PermissaoNegocio.EquipeGerenciar, permissao!.Permissao);
        Assert.NotNull(modulo);
        Assert.Equal(ModuloAssinatura.Profissionais, modulo!.Modulo);
    }

    [Fact]
    public void AtualizarStatusUsuarioEquipe_DeveExigirPermissaoGerenciarEquipe()
    {
        var method = typeof(EstabelecimentosController).GetMethod(nameof(EstabelecimentosController.AtualizarStatusUsuarioEquipe));

        var permissao = method!.GetCustomAttributes<RequerPermissaoNegocioAttribute>()
            .SingleOrDefault();
        var modulo = method!.GetCustomAttributes<RequerModuloAssinaturaAttribute>()
            .SingleOrDefault();

        Assert.NotNull(permissao);
        Assert.Equal(PermissaoNegocio.EquipeGerenciar, permissao!.Permissao);
        Assert.NotNull(modulo);
        Assert.Equal(ModuloAssinatura.Profissionais, modulo!.Modulo);
    }

    [Fact]
    public void AtualizarStatusProfissionalEquipe_DeveExigirPermissaoGerenciarProfissional()
    {
        var method = typeof(EstabelecimentosController).GetMethod(nameof(EstabelecimentosController.AtualizarStatusProfissionalEquipe));

        var permissao = method!.GetCustomAttributes<RequerPermissaoNegocioAttribute>()
            .SingleOrDefault();
        var modulo = method!.GetCustomAttributes<RequerModuloAssinaturaAttribute>()
            .SingleOrDefault();

        Assert.NotNull(permissao);
        Assert.Equal(PermissaoNegocio.ProfissionalGerenciar, permissao!.Permissao);
        Assert.NotNull(modulo);
        Assert.Equal(ModuloAssinatura.Profissionais, modulo!.Modulo);
    }

    [Fact]
    public void ObterCaixa_DeveExigirModuloCaixaEPermissaoVisualizarCaixa()
    {
        var method = typeof(EstabelecimentosController).GetMethod(nameof(EstabelecimentosController.ObterCaixa));

        var permissao = method!.GetCustomAttributes<RequerPermissaoNegocioAttribute>()
            .SingleOrDefault();
        var modulo = method!.GetCustomAttributes<RequerModuloAssinaturaAttribute>()
            .SingleOrDefault();

        Assert.NotNull(permissao);
        Assert.Equal(PermissaoNegocio.CaixaVisualizar, permissao!.Permissao);
        Assert.Equal("estabelecimentoId", permissao.ParametroId);
        Assert.NotNull(modulo);
        Assert.Equal(ModuloAssinatura.Caixa, modulo!.Modulo);
        Assert.Equal("estabelecimentoId", modulo.ParametroId);
    }

    [Fact]
    public void ListarLancamentosCaixa_DeveExigirModuloCaixaEPermissaoVisualizarCaixa()
    {
        var method = typeof(EstabelecimentosController).GetMethod(nameof(EstabelecimentosController.ListarLancamentosCaixa));

        var permissao = method!.GetCustomAttributes<RequerPermissaoNegocioAttribute>()
            .SingleOrDefault();
        var modulo = method!.GetCustomAttributes<RequerModuloAssinaturaAttribute>()
            .SingleOrDefault();

        Assert.NotNull(permissao);
        Assert.Equal(PermissaoNegocio.CaixaVisualizar, permissao!.Permissao);
        Assert.NotNull(modulo);
        Assert.Equal(ModuloAssinatura.Caixa, modulo!.Modulo);
    }

    [Fact]
    public void VincularServicoProfissional_DeveExigirModuloServicosEPermissaoGerenciarServico()
    {
        var method = typeof(EstabelecimentosController).GetMethod(nameof(EstabelecimentosController.VincularServicoProfissional));

        var permissao = method!.GetCustomAttributes<RequerPermissaoNegocioAttribute>()
            .SingleOrDefault();
        var modulo = method!.GetCustomAttributes<RequerModuloAssinaturaAttribute>()
            .SingleOrDefault();

        Assert.NotNull(permissao);
        Assert.Equal(PermissaoNegocio.ServicoGerenciar, permissao!.Permissao);
        Assert.Equal("estabelecimentoId", permissao.ParametroId);
        Assert.NotNull(modulo);
        Assert.Equal(ModuloAssinatura.Servicos, modulo!.Modulo);
        Assert.Equal("estabelecimentoId", modulo.ParametroId);
    }

    [Fact]
    public void CriarHorarioProfissional_DeveExigirModuloHorariosEPermissaoGerenciarProfissional()
    {
        var method = typeof(EstabelecimentosController).GetMethod(nameof(EstabelecimentosController.CriarHorarioProfissional));

        var permissao = method!.GetCustomAttributes<RequerPermissaoNegocioAttribute>()
            .SingleOrDefault();
        var modulo = method!.GetCustomAttributes<RequerModuloAssinaturaAttribute>()
            .SingleOrDefault();

        Assert.NotNull(permissao);
        Assert.Equal(PermissaoNegocio.ProfissionalGerenciar, permissao!.Permissao);
        Assert.Equal("estabelecimentoId", permissao.ParametroId);
        Assert.NotNull(modulo);
        Assert.Equal(ModuloAssinatura.HorariosAtendimento, modulo!.Modulo);
        Assert.Equal("estabelecimentoId", modulo.ParametroId);
    }

    [Fact]
    public void AtualizarStatusHorarioProfissional_DeveExigirModuloHorariosEPermissaoGerenciarProfissional()
    {
        var method = typeof(EstabelecimentosController).GetMethod(nameof(EstabelecimentosController.AtualizarStatusHorarioProfissional));

        var permissao = method!.GetCustomAttributes<RequerPermissaoNegocioAttribute>()
            .SingleOrDefault();
        var modulo = method!.GetCustomAttributes<RequerModuloAssinaturaAttribute>()
            .SingleOrDefault();

        Assert.NotNull(permissao);
        Assert.Equal(PermissaoNegocio.ProfissionalGerenciar, permissao!.Permissao);
        Assert.NotNull(modulo);
        Assert.Equal(ModuloAssinatura.HorariosAtendimento, modulo!.Modulo);
    }
}
