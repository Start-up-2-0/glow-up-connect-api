using GLOWAPI.Application.Services;
using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Tests.Unit.Application;

public class MatrizPermissaoNegocioServiceTests
{
    private readonly MatrizPermissaoNegocioService _service = new();

    [Fact]
    public void ObterPermissoes_DeveLiberarTudoSensivelParaOwner()
    {
        var permissoes = _service.ObterPermissoes(EstablishmentUserRole.Owner);

        Assert.Contains(PermissaoNegocio.NegocioEditar, permissoes);
        Assert.Contains(PermissaoNegocio.EquipeGerenciar, permissoes);
        Assert.Contains(PermissaoNegocio.CaixaVisualizar, permissoes);
        Assert.Contains(PermissaoNegocio.CaixaGerenciar, permissoes);
        Assert.Contains(PermissaoNegocio.AgendaVisualizarGeral, permissoes);
    }

    [Fact]
    public void ObterPermissoes_DeveBloquearDadosSensiveisParaReceptionist()
    {
        var permissoes = _service.ObterPermissoes(EstablishmentUserRole.Receptionist);

        Assert.Contains(PermissaoNegocio.AgendaVisualizarGeral, permissoes);
        Assert.Contains(PermissaoNegocio.AgendaCriar, permissoes);
        Assert.Contains(PermissaoNegocio.AgendaReagendar, permissoes);
        Assert.Contains(PermissaoNegocio.AgendaCancelar, permissoes);
        Assert.Contains(PermissaoNegocio.ClienteVisualizarGeral, permissoes);
        Assert.DoesNotContain(PermissaoNegocio.CaixaVisualizar, permissoes);
        Assert.DoesNotContain(PermissaoNegocio.CaixaGerenciar, permissoes);
        Assert.DoesNotContain(PermissaoNegocio.EquipeGerenciar, permissoes);
        Assert.DoesNotContain(PermissaoNegocio.ProfissionalConvidar, permissoes);
        Assert.DoesNotContain(PermissaoNegocio.ProfissionalGerenciar, permissoes);
        Assert.DoesNotContain(PermissaoNegocio.NegocioEditar, permissoes);
    }

    [Fact]
    public void ObterPermissoesProfissional_DevePermitirSomenteOperacaoPropria()
    {
        var permissoes = _service.ObterPermissoesProfissional();

        Assert.Contains(PermissaoNegocio.AgendaVisualizarPropria, permissoes);
        Assert.Contains(PermissaoNegocio.AtendimentoIniciar, permissoes);
        Assert.Contains(PermissaoNegocio.AtendimentoFinalizar, permissoes);
        Assert.Contains(PermissaoNegocio.ClienteVisualizarProprio, permissoes);
        Assert.DoesNotContain(PermissaoNegocio.ComissaoVisualizarPropria, permissoes);
        Assert.DoesNotContain(PermissaoNegocio.AgendaVisualizarGeral, permissoes);
        Assert.DoesNotContain(PermissaoNegocio.ClienteVisualizarGeral, permissoes);
        Assert.DoesNotContain(PermissaoNegocio.CaixaVisualizar, permissoes);
        Assert.DoesNotContain(PermissaoNegocio.EquipeGerenciar, permissoes);
        Assert.DoesNotContain(PermissaoNegocio.NegocioEditar, permissoes);
    }

    [Fact]
    public void ObterPermissoes_DeveTratarRoleProfissionalComoOperacaoPropria()
    {
        var permissoes = _service.ObterPermissoes(EstablishmentUserRole.Profissional);

        Assert.Contains(PermissaoNegocio.AgendaVisualizarPropria, permissoes);
        Assert.Contains(PermissaoNegocio.AtendimentoIniciar, permissoes);
        Assert.DoesNotContain(PermissaoNegocio.AgendaVisualizarGeral, permissoes);
        Assert.DoesNotContain(PermissaoNegocio.CaixaVisualizar, permissoes);
        Assert.DoesNotContain(PermissaoNegocio.EquipeGerenciar, permissoes);
    }

    [Fact]
    public void ObterPermissoes_DeveUnirPermissoesQuandoUsuarioTambemForProfissional()
    {
        var permissoes = _service.ObterPermissoes(
            EstablishmentUserRole.Admin,
            possuiVinculoProfissional: true);

        Assert.Contains(PermissaoNegocio.AgendaVisualizarGeral, permissoes);
        Assert.Contains(PermissaoNegocio.AgendaVisualizarPropria, permissoes);
        Assert.Contains(PermissaoNegocio.AtendimentoIniciar, permissoes);
        Assert.Contains(PermissaoNegocio.CaixaVisualizar, permissoes);
    }

    [Fact]
    public void ObterPermissoes_DeveBloquearCaixaParaManagerPorPadrao()
    {
        var permissoes = _service.ObterPermissoes(EstablishmentUserRole.Manager);

        Assert.Contains(PermissaoNegocio.AgendaVisualizarGeral, permissoes);
        Assert.Contains(PermissaoNegocio.ProfissionalGerenciar, permissoes);
        Assert.DoesNotContain(PermissaoNegocio.CaixaVisualizar, permissoes);
        Assert.DoesNotContain(PermissaoNegocio.CaixaGerenciar, permissoes);
    }

    [Fact]
    public void ObterPermissoes_DevePermitirCaixaParaManagerSomenteQuandoExplicito()
    {
        var permissoes = _service.ObterPermissoes(
            EstablishmentUserRole.Manager,
            managerPodeAcessarCaixa: true);

        Assert.Contains(PermissaoNegocio.CaixaVisualizar, permissoes);
        Assert.Contains(PermissaoNegocio.CaixaGerenciar, permissoes);
    }

    [Fact]
    public void PossuiPermissao_DeveConsultarPermissaoEfetiva()
    {
        var possui = _service.PossuiPermissao(
            EstablishmentUserRole.Receptionist,
            PermissaoNegocio.AgendaCriar);

        var naoPossui = _service.PossuiPermissao(
            EstablishmentUserRole.Receptionist,
            PermissaoNegocio.CaixaVisualizar);

        Assert.True(possui);
        Assert.False(naoPossui);
    }
}
