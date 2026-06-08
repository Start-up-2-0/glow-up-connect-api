using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Application.Services;

public class MatrizPermissaoNegocioService : IMatrizPermissaoNegocioService
{
    private static readonly IReadOnlySet<PermissaoNegocio> PermissoesOwner = new HashSet<PermissaoNegocio>
    {
        PermissaoNegocio.NegocioVisualizar,
        PermissaoNegocio.NegocioEditar,
        PermissaoNegocio.EquipeVisualizar,
        PermissaoNegocio.EquipeGerenciar,
        PermissaoNegocio.ProfissionalConvidar,
        PermissaoNegocio.ProfissionalGerenciar,
        PermissaoNegocio.ServicoVisualizar,
        PermissaoNegocio.ServicoGerenciar,
        PermissaoNegocio.HorarioVisualizar,
        PermissaoNegocio.HorarioGerenciar,
        PermissaoNegocio.AgendaVisualizarGeral,
        PermissaoNegocio.AgendaCriar,
        PermissaoNegocio.AgendaReagendar,
        PermissaoNegocio.AgendaCancelar,
        PermissaoNegocio.ClienteVisualizarGeral,
        PermissaoNegocio.CaixaVisualizar,
        PermissaoNegocio.CaixaGerenciar
    };

    private static readonly IReadOnlySet<PermissaoNegocio> PermissoesAdmin = PermissoesOwner;

    private static readonly IReadOnlySet<PermissaoNegocio> PermissoesManager = new HashSet<PermissaoNegocio>
    {
        PermissaoNegocio.NegocioVisualizar,
        PermissaoNegocio.EquipeVisualizar,
        PermissaoNegocio.ProfissionalConvidar,
        PermissaoNegocio.ProfissionalGerenciar,
        PermissaoNegocio.ServicoVisualizar,
        PermissaoNegocio.ServicoGerenciar,
        PermissaoNegocio.HorarioVisualizar,
        PermissaoNegocio.HorarioGerenciar,
        PermissaoNegocio.AgendaVisualizarGeral,
        PermissaoNegocio.AgendaCriar,
        PermissaoNegocio.AgendaReagendar,
        PermissaoNegocio.AgendaCancelar,
        PermissaoNegocio.ClienteVisualizarGeral
    };

    private static readonly IReadOnlySet<PermissaoNegocio> PermissoesReceptionist = new HashSet<PermissaoNegocio>
    {
        PermissaoNegocio.NegocioVisualizar,
        PermissaoNegocio.ServicoVisualizar,
        PermissaoNegocio.HorarioVisualizar,
        PermissaoNegocio.AgendaVisualizarGeral,
        PermissaoNegocio.AgendaCriar,
        PermissaoNegocio.AgendaReagendar,
        PermissaoNegocio.AgendaCancelar,
        PermissaoNegocio.ClienteVisualizarGeral
    };

    private static readonly IReadOnlySet<PermissaoNegocio> PermissoesProfissional = new HashSet<PermissaoNegocio>
    {
        PermissaoNegocio.NegocioVisualizar,
        PermissaoNegocio.ServicoVisualizar,
        PermissaoNegocio.HorarioVisualizar,
        PermissaoNegocio.HorarioGerenciarProprio,
        PermissaoNegocio.AgendaVisualizarPropria,
        PermissaoNegocio.AtendimentoVisualizarProprio,
        PermissaoNegocio.AtendimentoIniciar,
        PermissaoNegocio.AtendimentoFinalizar,
        PermissaoNegocio.ClienteVisualizarProprio
    };

    public IReadOnlySet<PermissaoNegocio> ObterPermissoes(
        EstablishmentUserRole role,
        bool possuiVinculoProfissional = false,
        bool managerPodeAcessarCaixa = false)
    {
        var permissoes = new HashSet<PermissaoNegocio>(ObterPermissoesPorRole(role));

        if (role == EstablishmentUserRole.Manager && managerPodeAcessarCaixa)
        {
            permissoes.Add(PermissaoNegocio.CaixaVisualizar);
            permissoes.Add(PermissaoNegocio.CaixaGerenciar);
        }

        if (possuiVinculoProfissional)
        {
            permissoes.UnionWith(PermissoesProfissional);
        }

        return permissoes;
    }

    public IReadOnlySet<PermissaoNegocio> ObterPermissoesProfissional()
    {
        return PermissoesProfissional;
    }

    public bool PossuiPermissao(
        EstablishmentUserRole role,
        PermissaoNegocio permissao,
        bool possuiVinculoProfissional = false,
        bool managerPodeAcessarCaixa = false)
    {
        return ObterPermissoes(role, possuiVinculoProfissional, managerPodeAcessarCaixa)
            .Contains(permissao);
    }

    private static IReadOnlySet<PermissaoNegocio> ObterPermissoesPorRole(EstablishmentUserRole role)
    {
        return role switch
        {
            EstablishmentUserRole.Owner => PermissoesOwner,
            EstablishmentUserRole.Admin => PermissoesAdmin,
            EstablishmentUserRole.Manager => PermissoesManager,
            EstablishmentUserRole.Receptionist => PermissoesReceptionist,
            EstablishmentUserRole.Profissional => PermissoesProfissional,
            _ => Array.Empty<PermissaoNegocio>().ToHashSet()
        };
    }
}
