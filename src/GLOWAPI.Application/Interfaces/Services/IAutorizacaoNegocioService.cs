using GLOWAPI.Application.Models.Autorizacao;
using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Application.Interfaces.Services;

public interface IAutorizacaoNegocioService
{
    Task<AutorizacaoNegocioResultado> ObterContextoAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken = default);

    Task<AutorizacaoNegocioResultado> ObterContextoPorPublicGuidAsync(
        Guid publicGuid,
        CancellationToken cancellationToken = default);

    Task<AutorizacaoNegocioResultado> AutorizarAsync(
        int estabelecimentoId,
        PermissaoNegocio permissao,
        CancellationToken cancellationToken = default);

    Task<AutorizacaoNegocioResultado> AutorizarPorPublicGuidAsync(
        Guid publicGuid,
        PermissaoNegocio permissao,
        CancellationToken cancellationToken = default);

    Task<bool> PossuiPermissaoAsync(
        int estabelecimentoId,
        PermissaoNegocio permissao,
        CancellationToken cancellationToken = default);
}
