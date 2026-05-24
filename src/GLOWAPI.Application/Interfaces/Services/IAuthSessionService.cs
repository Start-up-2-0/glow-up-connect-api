using GLOWAPI.Application.Models.Auth;
using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.Interfaces.Services;

public interface IAuthSessionService
{
    Task<IssuedTokenPair> CriarSessaoComTokensAsync(
        Usuario usuario,
        AuthSessionContext context,
        CancellationToken cancellationToken = default);

    Task<AuthenticatedSessionResult> ObterSessaoAtivaPorAccessTokenAsync(
        string accessToken,
        AuthSessionContext context,
        CancellationToken cancellationToken = default);

    Task<SessaoAutenticacao> ObterSessaoAtivaPorRefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);

    Task<SessaoAutenticacao?> ObterSessaoPorIdAsync(
        int sessionId,
        CancellationToken cancellationToken = default);

    Task RevogarSessaoAsync(SessaoAutenticacao sessao, CancellationToken cancellationToken = default);

    Task<IssuedTokenPair> RotacionarSessaoAsync(
        SessaoAutenticacao sessaoAtual,
        Usuario usuario,
        AuthSessionContext context,
        CancellationToken cancellationToken = default);

    Task RenovarExpiracaoAsync(SessaoAutenticacao sessao, CancellationToken cancellationToken = default);
}
