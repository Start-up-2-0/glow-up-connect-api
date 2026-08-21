using System.Security.Cryptography;
using GLOWAPI.Application.DTOs.Mensageria;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Mensageria;
using GLOWAPI.Application.Options;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Usuario;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GLOWAPI.Application.Services;

public class ConfirmacaoEmailService : IConfirmacaoEmailService
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IGlowTokenService _tokenService;
    private readonly IMensagemNotificacaoService _mensagemNotificacaoService;
    private readonly IAgendamentoConfirmacaoContaService _agendamentoConfirmacaoContaService;
    private readonly IConfirmacaoWhatsAppService _confirmacaoWhatsAppService;
    private readonly AuthOptions _authOptions;
    private readonly ILogger<ConfirmacaoEmailService> _logger;

    public ConfirmacaoEmailService(
        IUsuarioRepository usuarioRepository,
        IGlowTokenService tokenService,
        IMensagemNotificacaoService mensagemNotificacaoService,
        IAgendamentoConfirmacaoContaService agendamentoConfirmacaoContaService,
        IConfirmacaoWhatsAppService confirmacaoWhatsAppService,
        IOptions<AuthOptions> authOptions,
        ILogger<ConfirmacaoEmailService> logger)
    {
        _usuarioRepository = usuarioRepository;
        _tokenService = tokenService;
        _mensagemNotificacaoService = mensagemNotificacaoService;
        _agendamentoConfirmacaoContaService = agendamentoConfirmacaoContaService;
        _confirmacaoWhatsAppService = confirmacaoWhatsAppService;
        _authOptions = authOptions.Value;
        _logger = logger;
    }

    public async Task<(string TokenPlano, string CodigoPlano)> GerarEEnviarConfirmacaoAsync(
        Usuario usuario,
        CancellationToken cancellationToken = default)
    {
        var tokenPlano = _tokenService.GerarRefreshToken();
        var codigoPlano = GerarCodigoNumerico(_authOptions.ConfirmacaoCodigoDigitos);

        usuario.ConfirmacaoTokenHash = _tokenService.HashToken(tokenPlano);
        usuario.ConfirmacaoCodigoHash = _tokenService.HashToken(codigoPlano);
        usuario.ConfirmacaoExpiraEm = DateTime.UtcNow.AddHours(_authOptions.ConfirmacaoEmailHoras);
        usuario.UpdatedAt = DateTime.UtcNow;

        _usuarioRepository.Atualizar(usuario);
        await _usuarioRepository.SalvarAlteracoesAsync(cancellationToken);

        var link = $"{_authOptions.FrontendBaseUrl.TrimEnd('/')}/confirmar-email?token={Uri.EscapeDataString(tokenPlano)}";
        var conteudo = ConfirmacaoEmailTemplate.Criar(
            usuario.Nome,
            link,
            codigoPlano,
            _authOptions.ConfirmacaoEmailHoras);

        await _mensagemNotificacaoService.RegistrarAsync(new RegistrarMensagemNotificacaoDto
        {
            Canal = CanalMensagemNotificacao.Email,
            Destinatario = usuario.Email,
            Assunto = "Confirme seu cadastro",
            Conteudo = conteudo,
            Prioridade = 2
        }, cancellationToken);

        return (tokenPlano, codigoPlano);
    }

    public async Task ConfirmarPorTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ConfirmacaoEmailInvalidaException();
        }

        var hash = _tokenService.HashToken(token.Trim());
        var usuario = await _usuarioRepository.ObterPorConfirmacaoTokenHashAsync(hash, cancellationToken);
        await AtivarUsuarioConfirmadoAsync(usuario, cancellationToken);
    }

    public async Task ConfirmarPorCodigoAsync(string codigo, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(codigo))
        {
            throw new ConfirmacaoEmailInvalidaException();
        }

        var codigoNormalizado = codigo.Trim();
        var hash = _tokenService.HashToken(codigoNormalizado);
        var usuario = await _usuarioRepository.ObterPorConfirmacaoCodigoHashAsync(hash, cancellationToken);
        await AtivarUsuarioConfirmadoAsync(usuario, cancellationToken);
    }

    public async Task ReenviarConfirmacaoAsync(string email, CancellationToken cancellationToken = default)
    {
        var emailNormalizado = NormalizarEmail(email);
        if (string.IsNullOrEmpty(emailNormalizado))
        {
            return;
        }

        var usuario = await _usuarioRepository.ObterPorEmailAsync(emailNormalizado, cancellationToken);
        if (usuario is null || usuario.Ativo)
        {
            return;
        }

        await GerarEEnviarConfirmacaoAsync(usuario, cancellationToken);
    }

    private async Task AtivarUsuarioConfirmadoAsync(Usuario? usuario, CancellationToken cancellationToken)
    {
        if (usuario is null
            || usuario.ConfirmacaoExpiraEm is null
            || usuario.ConfirmacaoExpiraEm <= DateTime.UtcNow)
        {
            throw new ConfirmacaoEmailInvalidaException();
        }

        if (usuario.Ativo)
        {
            return;
        }

        usuario.Ativo = true;
        usuario.LimparConfirmacaoEmail();
        usuario.UpdatedAt = DateTime.UtcNow;

        _usuarioRepository.Atualizar(usuario);
        await _usuarioRepository.SalvarAlteracoesAsync(cancellationToken);

        await _agendamentoConfirmacaoContaService.ProcessarConfirmacaoContaClienteAsync(
            usuario.Id,
            cancellationToken);

        await IniciarConfirmacaoWhatsAppSePendenteAsync(usuario, cancellationToken);
    }

    private async Task IniciarConfirmacaoWhatsAppSePendenteAsync(
        Usuario usuario,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(usuario.Telefone) || usuario.WhatsAppConfirmadoEm.HasValue)
        {
            return;
        }

        try
        {
            await _confirmacaoWhatsAppService.IniciarConfirmacaoAsync(usuario, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "E-mail confirmado, mas nao foi possivel iniciar a confirmacao de WhatsApp. UsuarioId={UsuarioId}",
                usuario.Id);
        }
    }

    private static string GerarCodigoNumerico(int digitos)
    {
        var max = (int)Math.Pow(10, digitos);
        var valor = RandomNumberGenerator.GetInt32(0, max);
        return valor.ToString($"D{digitos}");
    }

    public static string NormalizarEmail(string email) =>
        email.Trim().ToLowerInvariant();
}
