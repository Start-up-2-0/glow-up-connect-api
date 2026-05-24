using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Models.Auth;
using GLOWAPI.Application.Options;
using GLOWAPI.Domain.Exceptions.Auth;
using Microsoft.Extensions.Options;

namespace GLOWAPI.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IAuthSessionService _authSessionService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ISecurityAuditLogger _auditLogger;
    private readonly AuthOptions _authOptions;

    public AuthService(
        IUsuarioRepository usuarioRepository,
        IAuthSessionService authSessionService,
        IPasswordHasher passwordHasher,
        ISecurityAuditLogger auditLogger,
        IOptions<AuthOptions> authOptions)
    {
        _usuarioRepository = usuarioRepository;
        _authSessionService = authSessionService;
        _passwordHasher = passwordHasher;
        _auditLogger = auditLogger;
        _authOptions = authOptions.Value;
    }

    public async Task<AuthLoginResult> LoginAsync(
        string email,
        string senha,
        AuthSessionContext context,
        CancellationToken cancellationToken = default)
    {
        var usuario = await _usuarioRepository.ObterPorEmailAsync(email, cancellationToken);

        if (usuario is null)
        {
            await _auditLogger.LoginFailedAsync(email, "usuario_inexistente", context.Ip, context.UserAgent, cancellationToken: cancellationToken);
            throw new InvalidCredentialsException();
        }

        if (!usuario.Ativo)
        {
            await _auditLogger.LoginFailedAsync(email, "usuario_inativo", context.Ip, context.UserAgent, usuario.Id, cancellationToken);
            throw new InactiveUserException();
        }

        if (usuario.EstaBloqueado(_authOptions.MaxLoginAttempts))
        {
            await _auditLogger.UserBlockedAsync(usuario.Id, email, context.Ip, context.UserAgent, cancellationToken);
            throw new UserBlockedException();
        }

        if (!_passwordHasher.Verify(senha, usuario.Senha))
        {
            usuario.RegistrarTentativaFalha();

            if (usuario.Tentativas >= _authOptions.MaxLoginAttempts)
            {
                usuario.AplicarBloqueioTemporario(_authOptions.LockoutMinutes);
                await _auditLogger.UserBlockedAsync(usuario.Id, email, context.Ip, context.UserAgent, cancellationToken);
            }
            else
            {
                await _auditLogger.LoginFailedAsync(email, "senha_invalida", context.Ip, context.UserAgent, usuario.Id, cancellationToken);
            }

            _usuarioRepository.Atualizar(usuario);
            await _usuarioRepository.SalvarAlteracoesAsync(cancellationToken);
            throw new InvalidCredentialsException();
        }

        usuario.ResetarTentativas();
        _usuarioRepository.Atualizar(usuario);
        await _usuarioRepository.SalvarAlteracoesAsync(cancellationToken);

        var tokens = await _authSessionService.CriarSessaoComTokensAsync(usuario, context, cancellationToken);

        await _auditLogger.LoginSucceededAsync(usuario.Id, usuario.Email, context.Ip, context.UserAgent, cancellationToken);

        return new AuthLoginResult(
            tokens.AccessToken,
            tokens.RefreshToken,
            tokens.AccessTokenExpiresAt,
            tokens.RefreshTokenExpiresAt,
            new UsuarioAuthInfo(usuario.Id, usuario.Nome, usuario.Email, usuario.Role));
    }

    public async Task LogoutAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        var auth = await _authSessionService.ObterSessaoAtivaPorAccessTokenAsync(
            accessToken,
            new AuthSessionContext(null, null),
            cancellationToken);

        var sessao = await _authSessionService.ObterSessaoPorIdAsync(auth.Sessao.Id, cancellationToken);
        if (sessao is null)
        {
            throw new InvalidTokenException();
        }

        await _authSessionService.RevogarSessaoAsync(sessao, cancellationToken);
        await _auditLogger.LogoutAsync(auth.Usuario.Id, auth.Sessao.Id, null, null, cancellationToken);
    }

    public async Task<AuthRefreshResult> RefreshAsync(
        string refreshToken,
        AuthSessionContext context,
        CancellationToken cancellationToken = default)
    {
        var sessao = await _authSessionService.ObterSessaoAtivaPorRefreshTokenAsync(refreshToken, cancellationToken);
        var usuario = await _usuarioRepository.ObterPorIdAsync(sessao.UsuarioId, cancellationToken);

        if (usuario is null || !usuario.PodeAutenticar(_authOptions.MaxLoginAttempts))
        {
            await _authSessionService.RevogarSessaoAsync(sessao, cancellationToken);

            if (usuario is not null && usuario.EstaBloqueado(_authOptions.MaxLoginAttempts))
            {
                throw new UserBlockedException();
            }

            throw new InactiveUserException();
        }

        var tokens = await _authSessionService.RotacionarSessaoAsync(sessao, usuario, context, cancellationToken);

        await _auditLogger.TokenRefreshedAsync(usuario.Id, tokens.SessionId, context.Ip, context.UserAgent, cancellationToken);

        return new AuthRefreshResult(
            tokens.AccessToken,
            tokens.RefreshToken,
            tokens.AccessTokenExpiresAt,
            tokens.RefreshTokenExpiresAt);
    }
}
