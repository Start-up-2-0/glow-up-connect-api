using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Application.Interfaces.Services;

public interface IMatrizPermissaoNegocioService
{
    IReadOnlySet<PermissaoNegocio> ObterPermissoes(
        EstablishmentUserRole role,
        bool possuiVinculoProfissional = false,
        bool managerPodeAcessarCaixa = false);

    IReadOnlySet<PermissaoNegocio> ObterPermissoesProfissional();

    bool PossuiPermissao(
        EstablishmentUserRole role,
        PermissaoNegocio permissao,
        bool possuiVinculoProfissional = false,
        bool managerPodeAcessarCaixa = false);
}
