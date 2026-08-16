using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Application.Interfaces.Repositories;

public interface IConviteNegocioRepository : IRepository<ConviteNegocio>
{
    Task<ConviteNegocio?> ObterPorTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ConviteNegocio>> ListarPorEstabelecimentoAsync(
        int estabelecimentoId,
        StatusConviteNegocio? status,
        CancellationToken cancellationToken = default);

    Task<bool> UsuarioJaUtilizouAsync(
        int conviteId,
        int usuarioId,
        CancellationToken cancellationToken = default);

    Task AdicionarUtilizacaoAsync(
        ConviteNegocioUtilizacao utilizacao,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Incrementa utilizações atomicamente se o convite ainda estiver ativo, no prazo e com vagas.
    /// Retorna false se a condição não foi satisfeita (corrida / esgotado / expirado).
    /// </summary>
    Task<bool> TentarRegistrarUtilizacaoAsync(
        int conviteId,
        DateTime agoraUtc,
        CancellationToken cancellationToken = default);

    Task CompensarUtilizacaoAsync(
        int conviteId,
        DateTime agoraUtc,
        CancellationToken cancellationToken = default);
}
