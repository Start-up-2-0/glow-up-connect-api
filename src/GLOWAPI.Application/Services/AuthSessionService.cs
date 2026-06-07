using System.Text.Json;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Models.Auth;
using GLOWAPI.Application.Options;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Exceptions.Auth;
using Microsoft.Extensions.Options;

namespace GLOWAPI.Application.Services;

public class AuthSessionService : IAuthSessionService
{
    private readonly ISessaoAutenticacaoRepository _sessaoRepository;
    private readonly IGlowTokenService _tokenService;
    private readonly AuthOptions _authOptions;

    public AuthSessionService(
        ISessaoAutenticacaoRepository sessaoRepository,
        IGlowTokenService tokenService,
        IOptions<AuthOptions> authOptions)
    {
        _sessaoRepository = sessaoRepository;
        _tokenService = tokenService;
        _authOptions = authOptions.Value;
    }

    public async Task<IssuedTokenPair> CriarSessaoComTokensAsync(
        Usuario usuario,
        AuthSessionContext context,
        CancellationToken cancellationToken = default)
    {
        var agora = DateTime.UtcNow;
        var refreshToken = _tokenService.GerarRefreshToken();

        var sessao = new SessaoAutenticacao
        {
            UsuarioId = usuario.Id,
            RefreshTokenHash = _tokenService.HashToken(refreshToken),
            LoginEm = agora,
            ExpiraEm = _tokenService.ObterExpiracaoRefreshToken(agora),
            Ip = context.Ip,
            UserAgent = context.UserAgent,
            MetadataJson = JsonSerializer.Serialize(new
            {
                userId = usuario.Id,
                email = usuario.Email,
                issuedAt = agora
            })
        };

        await _sessaoRepository.AdicionarAsync(sessao, cancellationToken);
        await _sessaoRepository.SalvarAlteracoesAsync(cancellationToken);

        var accessToken = _tokenService.EmitirAccessToken(usuario, sessao.Id, agora);
        sessao.AccessTokenHash = _tokenService.HashToken(accessToken);
        sessao.AccessTokenExpiraEm = _tokenService.ObterExpiracaoAccessToken(agora);

        _sessaoRepository.Atualizar(sessao);
        await _sessaoRepository.SalvarAlteracoesAsync(cancellationToken);

        return new IssuedTokenPair(
            accessToken,
            refreshToken,
            sessao.AccessTokenExpiraEm,
            sessao.ExpiraEm,
            sessao.Id);
    }

    public async Task<AuthenticatedSessionResult> ObterSessaoAtivaPorAccessTokenAsync(
        string accessToken,
        AuthSessionContext context,
        CancellationToken cancellationToken = default)
    {
        var metadata = _tokenService.ValidarMetadata(accessToken);
        if (metadata is null)
        {
            throw new InvalidTokenException();
        }

        if (_tokenService.EstaExpirado(metadata, DateTime.UtcNow))
        {
            throw new TokenExpiredException();
        }

        var hash = _tokenService.HashToken(accessToken);
        var sessao = await _sessaoRepository.ObterAtivaPorAccessTokenHashAsync(
            hash,
            metadata.SessionId,
            cancellationToken);

        if (sessao is null || sessao.UsuarioId != metadata.UserId)
        {
            throw new InvalidTokenException();
        }

        ValidarContextoSessao(sessao, context);

        var usuario = sessao.Usuario;
        if (!usuario.PodeAutenticarOnboarding(_authOptions.MaxLoginAttempts))
        {
            if (usuario.EstaBloqueado(_authOptions.MaxLoginAttempts))
            {
                throw new UserBlockedException();
            }

            throw new InactiveUserException();
        }

        if (usuario.EstaBloqueado(_authOptions.MaxLoginAttempts))
        {
            throw new UserBlockedException();
        }

        return new AuthenticatedSessionResult(
            new SessaoAutenticacaoInfo(sessao.Id, sessao.UsuarioId, sessao.Ip, sessao.UserAgent),
            new UsuarioAuthInfo(usuario.Id, usuario.Nome, usuario.Email, usuario.Role, usuario.AvatarBase64));
    }

    public async Task<SessaoAutenticacao> ObterSessaoAtivaPorRefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        var hash = _tokenService.HashToken(refreshToken);
        var sessao = await _sessaoRepository.ObterAtivaPorRefreshTokenHashAsync(hash, cancellationToken);

        if (sessao is null)
        {
            throw new InvalidTokenException();
        }

        if (!sessao.EstaAtiva(DateTime.UtcNow))
        {
            throw new TokenExpiredException();
        }

        return sessao;
    }

    public Task<SessaoAutenticacao?> ObterSessaoPorIdAsync(
        int sessionId,
        CancellationToken cancellationToken = default) =>
        _sessaoRepository.ObterPorIdAsync(sessionId, cancellationToken);

    public async Task RevogarSessaoAsync(SessaoAutenticacao sessao, CancellationToken cancellationToken = default)
    {
        sessao.Revogar(DateTime.UtcNow);
        _sessaoRepository.Atualizar(sessao);
        await _sessaoRepository.SalvarAlteracoesAsync(cancellationToken);
    }

    public async Task<IssuedTokenPair> RotacionarSessaoAsync(
        SessaoAutenticacao sessaoAtual,
        Usuario usuario,
        AuthSessionContext context,
        CancellationToken cancellationToken = default)
    {
        await RevogarSessaoAsync(sessaoAtual, cancellationToken);
        return await CriarSessaoComTokensAsync(usuario, context, cancellationToken);
    }

    public async Task RenovarExpiracaoAsync(SessaoAutenticacao sessao, CancellationToken cancellationToken = default)
    {
        var agora = DateTime.UtcNow;
        sessao.RenovarExpiracao(agora, _tokenService.ObterExpiracaoRefreshToken(agora));
        _sessaoRepository.Atualizar(sessao);
        await _sessaoRepository.SalvarAlteracoesAsync(cancellationToken);
    }

    public async Task RevogarTodasSessoesAsync(int usuarioId, CancellationToken cancellationToken = default)
    {
        var sessoesAtivas = await _sessaoRepository.ListarAtivasPorUsuarioIdAsync(usuarioId, cancellationToken);
        var agora = DateTime.UtcNow;

        foreach (var sessao in sessoesAtivas)
        {
            sessao.Revogar(agora);
            _sessaoRepository.Atualizar(sessao);
        }

        await _sessaoRepository.SalvarAlteracoesAsync(cancellationToken);
    }

    private void ValidarContextoSessao(SessaoAutenticacao sessao, AuthSessionContext context)
    {
        if (_authOptions.ValidateIpOnToken &&
            !string.IsNullOrWhiteSpace(sessao.Ip) &&
            !string.Equals(sessao.Ip, context.Ip, StringComparison.Ordinal))
        {
            throw new InvalidTokenException();
        }

        if (_authOptions.ValidateUserAgentOnToken &&
            !string.IsNullOrWhiteSpace(sessao.UserAgent) &&
            !string.Equals(sessao.UserAgent, context.UserAgent, StringComparison.Ordinal))
        {
            throw new InvalidTokenException();
        }
    }
}
