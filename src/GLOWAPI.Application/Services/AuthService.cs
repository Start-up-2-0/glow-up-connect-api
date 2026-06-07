using System.Security.Cryptography;
using GLOWAPI.Application.DTOs.Auth;
using GLOWAPI.Application.DTOs.Mensageria;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Models.Auth;
using GLOWAPI.Application.Options;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
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
    private readonly IRecuperacaoSenhaRepository _recuperacaoSenhaRepository;
    private readonly IMensagemNotificacaoService _mensagemNotificacaoService;

    public AuthService(
        IUsuarioRepository usuarioRepository,
        IAuthSessionService authSessionService,
        IPasswordHasher passwordHasher,
        ISecurityAuditLogger auditLogger,
        IRecuperacaoSenhaRepository recuperacaoSenhaRepository,
        IMensagemNotificacaoService mensagemNotificacaoService,
        IOptions<AuthOptions> authOptions)
    {
        _usuarioRepository = usuarioRepository;
        _authSessionService = authSessionService;
        _passwordHasher = passwordHasher;
        _auditLogger = auditLogger;
        _recuperacaoSenhaRepository = recuperacaoSenhaRepository;
        _mensagemNotificacaoService = mensagemNotificacaoService;
        _authOptions = authOptions.Value;
    }

    public async Task<AuthLoginResult> LoginAsync(
        LoginRequestDto dto,
        AuthSessionContext context,
        CancellationToken cancellationToken = default)
    {
        var email = ConfirmacaoEmailService.NormalizarEmail(dto.Email);
        var senha = dto.Senha;
        var usuario = await _usuarioRepository.ObterPorEmailAsync(email, cancellationToken);

        if (usuario is null)
        {
            await _auditLogger.LoginFailedAsync(email, "usuario_inexistente", context.Ip, context.UserAgent, cancellationToken: cancellationToken);
            throw new InvalidCredentialsException();
        }

        var requerConfirmacaoEmail = !usuario.Ativo && usuario.PendenteConfirmacaoEmail();

        if (!usuario.Ativo && !requerConfirmacaoEmail)
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
            new UsuarioAuthInfo(usuario.Id, usuario.Nome, usuario.Email, usuario.Role, usuario.AvatarBase64),
            requerConfirmacaoEmail);
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
        RefreshTokenRequestDto dto,
        AuthSessionContext context,
        CancellationToken cancellationToken = default)
    {
        var sessao = await _authSessionService.ObterSessaoAtivaPorRefreshTokenAsync(dto.RefreshToken, cancellationToken);
        var usuario = await _usuarioRepository.ObterPorIdAsync(sessao.UsuarioId, cancellationToken);

        if (usuario is null || !usuario.PodeAutenticarOnboarding(_authOptions.MaxLoginAttempts))
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

    public async Task ForgotPasswordAsync(
        ForgotPasswordRequestDto dto,
        string? ip,
        CancellationToken cancellationToken = default)
    {
        var email = ConfirmacaoEmailService.NormalizarEmail(dto.Email);
        if (string.IsNullOrWhiteSpace(email))
            return;

        var usuario = await _usuarioRepository.ObterPorEmailAsync(email, cancellationToken);
        if (usuario is null || !usuario.Ativo)
            return;

        var codigo = GerarCodigoNumerico(_authOptions.ConfirmacaoCodigoDigitos);
        var codigoHash = _passwordHasher.Hash(codigo);

        var recuperacao = new RecuperacaoSenha
        {
            UsuarioId = usuario.Id,
            CodigoHash = codigoHash,
            CodigoExpiraEm = DateTime.UtcNow.AddMinutes(_authOptions.CodigoExpiracaoMinutos),
            CodigoTentativas = 0,
            IpSolicitacao = ip
        };

        await _recuperacaoSenhaRepository.AdicionarAsync(recuperacao, cancellationToken);
        await _recuperacaoSenhaRepository.SalvarAlteracoesAsync(cancellationToken);

        var conteudo = $"Seu código de recuperação é: {codigo}\nVálido por {_authOptions.CodigoExpiracaoMinutos} minutos.";
        await _mensagemNotificacaoService.RegistrarAsync(new RegistrarMensagemNotificacaoDto
        {
            Canal = CanalMensagemNotificacao.Email,
            Destinatario = usuario.Email,
            Assunto = "Recuperação de senha",
            Conteudo = conteudo,
            Prioridade = 2
        }, cancellationToken);
    }

    public async Task<VerifyRecoveryCodeResult> VerifyRecoveryCodeAsync(
        VerifyRecoveryCodeRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        var codigo = dto.Codigo?.Trim();
        if (string.IsNullOrWhiteSpace(codigo))
            throw new CodigoRecuperacaoInvalidoException();

        var codigoHash = _passwordHasher.Hash(codigo);

        var email = ConfirmacaoEmailService.NormalizarEmail(dto.Email);
        var usuario = await _usuarioRepository.ObterPorEmailAsync(email, cancellationToken);
        if (usuario is null)
            throw new CodigoRecuperacaoInvalidoException();

        var recuperacao = await _recuperacaoSenhaRepository.ObterUltimoPorUsuarioAsync(usuario.Id, cancellationToken);
        if (recuperacao is null)
            throw new CodigoRecuperacaoInvalidoException();

        if (recuperacao.CodigoExpiraEm <= DateTime.UtcNow)
            throw new CodigoRecuperacaoExpiradoException();

        if (recuperacao.CodigoTentativas >= _authOptions.MaxTentativasCodigo)
            throw new CodigoRecuperacaoInvalidoException();

        if (recuperacao.CodigoHash != codigoHash)
        {
            recuperacao.CodigoTentativas++;
            _recuperacaoSenhaRepository.Atualizar(recuperacao);
            await _recuperacaoSenhaRepository.SalvarAlteracoesAsync(cancellationToken);
            throw new CodigoRecuperacaoInvalidoException();
        }

        recuperacao.CodigoVerificado = true;

        var resetToken = Guid.NewGuid().ToString("N");
        var resetTokenHash = _passwordHasher.Hash(resetToken);
        recuperacao.ResetTokenHash = resetTokenHash;
        recuperacao.ResetTokenExpiraEm = DateTime.UtcNow.AddMinutes(_authOptions.ResetTokenExpiracaoMinutos);
        recuperacao.ResetTokenConsumido = false;

        _recuperacaoSenhaRepository.Atualizar(recuperacao);
        await _recuperacaoSenhaRepository.SalvarAlteracoesAsync(cancellationToken);

        return new VerifyRecoveryCodeResult(resetToken);
    }

    public async Task ResetPasswordAsync(
        ResetPasswordRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        var resetToken = dto.ResetToken?.Trim();
        if (string.IsNullOrWhiteSpace(resetToken))
            throw new ResetTokenInvalidoException();

        var resetTokenHash = _passwordHasher.Hash(resetToken);

        var recuperacao = await _recuperacaoSenhaRepository.ObterPorResetTokenHashAsync(resetTokenHash, cancellationToken);
        if (recuperacao is null || recuperacao.ResetTokenConsumido)
            throw new ResetTokenInvalidoException();

        if (recuperacao.ResetTokenExpiraEm <= DateTime.UtcNow)
            throw new ResetTokenExpiradoException();

        var usuario = await _usuarioRepository.ObterPorIdAsync(recuperacao.UsuarioId, cancellationToken);
        if (usuario is null)
            throw new ResetTokenInvalidoException();

        usuario.Senha = _passwordHasher.Hash(dto.NovaSenha);
        usuario.UpdatedAt = DateTime.UtcNow;
        usuario.ResetarTentativas();

        recuperacao.ResetTokenConsumido = true;

        _usuarioRepository.Atualizar(usuario);
        _recuperacaoSenhaRepository.Atualizar(recuperacao);
        await _usuarioRepository.SalvarAlteracoesAsync(cancellationToken);
        await _recuperacaoSenhaRepository.SalvarAlteracoesAsync(cancellationToken);

        await _authSessionService.RevogarTodasSessoesAsync(usuario.Id, cancellationToken);
    }

    private static string GerarCodigoNumerico(int digitos)
    {
        var max = (int)Math.Pow(10, digitos);
        var valor = RandomNumberGenerator.GetInt32(0, max);
        return valor.ToString($"D{digitos}");
    }
}