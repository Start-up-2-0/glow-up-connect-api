using System.Security.Cryptography;
using GLOWAPI.Application.DTOs.Auth;
using GLOWAPI.Application.DTOs.Mensageria;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Mensageria;
using GLOWAPI.Application.Options;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Usuario;
using Microsoft.Extensions.Options;

namespace GLOWAPI.Application.Services;

public class RecuperacaoSenhaService : IRecuperacaoSenhaService
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IGlowTokenService _tokenService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IMensagemNotificacaoService _mensagemNotificacaoService;
    private readonly IAuthSessionService _authSessionService;
    private readonly AuthOptions _authOptions;

    public RecuperacaoSenhaService(
        IUsuarioRepository usuarioRepository,
        IGlowTokenService tokenService,
        IPasswordHasher passwordHasher,
        IMensagemNotificacaoService mensagemNotificacaoService,
        IAuthSessionService authSessionService,
        IOptions<AuthOptions> authOptions)
    {
        _usuarioRepository = usuarioRepository;
        _tokenService = tokenService;
        _passwordHasher = passwordHasher;
        _mensagemNotificacaoService = mensagemNotificacaoService;
        _authSessionService = authSessionService;
        _authOptions = authOptions.Value;
    }

    public async Task SolicitarAsync(string email, CancellationToken cancellationToken = default)
    {
        var emailNormalizado = NormalizarEmail(email);
        if (string.IsNullOrEmpty(emailNormalizado))
        {
            return;
        }

        var usuario = await _usuarioRepository.ObterPorEmailAsync(emailNormalizado, cancellationToken);
        if (usuario is null
            || !usuario.Ativo
            || usuario.ExclusaoPendenteVencida(DateTime.UtcNow))
        {
            return;
        }

        await GerarEEnviarRecuperacaoAsync(usuario, cancellationToken);
    }

    public async Task RedefinirAsync(ResetPasswordRequestDto request, CancellationToken cancellationToken = default)
    {
        var temToken = !string.IsNullOrWhiteSpace(request.Token);
        var temCodigo = !string.IsNullOrWhiteSpace(request.Codigo);
        if (temToken == temCodigo)
        {
            throw new ResetSenhaInvalidoException();
        }

        Usuario? usuario;
        if (temToken)
        {
            var hash = _tokenService.HashToken(request.Token!.Trim());
            usuario = await _usuarioRepository.ObterPorRecuperacaoTokenHashAsync(hash, cancellationToken);
        }
        else
        {
            var hash = _tokenService.HashToken(request.Codigo!.Trim());
            usuario = await _usuarioRepository.ObterPorRecuperacaoCodigoHashAsync(hash, cancellationToken);
        }

        await AplicarNovaSenhaAsync(usuario, request.Senha, request.ConfirmarSenha, cancellationToken);
    }

    private async Task GerarEEnviarRecuperacaoAsync(Usuario usuario, CancellationToken cancellationToken)
    {
        var tokenPlano = _tokenService.GerarRefreshToken();
        var codigoPlano = GerarCodigoNumerico(_authOptions.ConfirmacaoCodigoDigitos);

        usuario.RecuperacaoTokenHash = _tokenService.HashToken(tokenPlano);
        usuario.RecuperacaoCodigoHash = _tokenService.HashToken(codigoPlano);
        usuario.RecuperacaoExpiraEm = DateTime.UtcNow.AddMinutes(_authOptions.RecuperacaoSenhaMinutos);
        usuario.UpdatedAt = DateTime.UtcNow;

        _usuarioRepository.Atualizar(usuario);
        await _usuarioRepository.SalvarAlteracoesAsync(cancellationToken);

        var link = $"{_authOptions.FrontendBaseUrl.TrimEnd('/')}/resetar-senha?token={Uri.EscapeDataString(tokenPlano)}";
        var conteudo = RecuperacaoSenhaTemplate.Criar(
            usuario.Nome,
            link,
            codigoPlano,
            _authOptions.RecuperacaoSenhaMinutos);

        await _mensagemNotificacaoService.RegistrarAsync(new RegistrarMensagemNotificacaoDto
        {
            Canal = CanalMensagemNotificacao.Email,
            Destinatario = usuario.Email,
            Assunto = "Redefina sua senha",
            Conteudo = conteudo,
            Prioridade = 3,
            MaximoTentativas = 3
        }, cancellationToken);
    }

    private async Task AplicarNovaSenhaAsync(
        Usuario? usuario,
        string senha,
        string confirmarSenha,
        CancellationToken cancellationToken)
    {
        if (usuario is null
            || usuario.RecuperacaoExpiraEm is null
            || usuario.RecuperacaoExpiraEm <= DateTime.UtcNow)
        {
            throw new ResetSenhaInvalidoException();
        }

        if (!string.Equals(senha, confirmarSenha, StringComparison.Ordinal))
        {
            throw new ResetSenhaInvalidoException("As senhas nao coincidem.");
        }

        if (_passwordHasher.Verify(senha, usuario.Senha))
        {
            throw new ResetSenhaInvalidoException("A nova senha deve ser diferente da senha anterior.");
        }

        usuario.Senha = _passwordHasher.Hash(senha);
        usuario.LimparRecuperacaoSenha();
        usuario.ResetarTentativas();
        usuario.UpdatedAt = DateTime.UtcNow;

        _usuarioRepository.Atualizar(usuario);
        await _usuarioRepository.SalvarAlteracoesAsync(cancellationToken);

        await _authSessionService.RevogarTodasSessoesDoUsuarioAsync(usuario.Id, cancellationToken);
    }

    private static string GerarCodigoNumerico(int digitos)
    {
        var max = (int)Math.Pow(10, digitos);
        var valor = RandomNumberGenerator.GetInt32(0, max);
        return valor.ToString($"D{digitos}");
    }

    private static string NormalizarEmail(string email) =>
        string.IsNullOrWhiteSpace(email) ? string.Empty : email.Trim().ToLowerInvariant();
}
