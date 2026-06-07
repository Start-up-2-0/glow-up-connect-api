using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.Interfaces.Repositories;

public interface ISessaoAutenticacaoRepository : IRepository<SessaoAutenticacao>
{
    Task<SessaoAutenticacao?> ObterAtivaPorRefreshTokenHashAsync(
        string refreshTokenHash,
        CancellationToken cancellationToken = default);

    Task<SessaoAutenticacao?> ObterAtivaPorAccessTokenHashAsync(
        string accessTokenHash,
        int sessionId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SessaoAutenticacao>> ListarAtivasPorUsuarioIdAsync(
        int usuarioId,
        CancellationToken cancellationToken = default);
}
