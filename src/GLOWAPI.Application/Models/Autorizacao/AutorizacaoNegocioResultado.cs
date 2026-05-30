using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Application.Models.Autorizacao;

public record AutorizacaoNegocioResultado(
    int EstabelecimentoId,
    int UsuarioId,
    EstablishmentUserRole Role,
    bool PossuiVinculoProfissional,
    IReadOnlySet<PermissaoNegocio> Permissoes)
{
    public bool PossuiPermissao(PermissaoNegocio permissao) => Permissoes.Contains(permissao);
}
