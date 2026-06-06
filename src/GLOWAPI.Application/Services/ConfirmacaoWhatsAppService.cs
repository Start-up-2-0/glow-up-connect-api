using System.Security.Cryptography;
using GLOWAPI.Application.DTOs.Mensageria;
using GLOWAPI.Application.Helpers;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Mensageria;
using GLOWAPI.Application.Models.Mensageria;
using GLOWAPI.Application.Options;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Usuario;
using Microsoft.Extensions.Options;

namespace GLOWAPI.Application.Services;

public class ConfirmacaoWhatsAppService : IConfirmacaoWhatsAppService
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IGlowTokenService _tokenService;
    private readonly IMensagemNotificacaoService _mensagemNotificacaoService;
    private readonly MensageriaWhatsAppOptions _whatsAppOptions;
    private readonly AuthOptions _authOptions;

    public ConfirmacaoWhatsAppService(
        IUsuarioRepository usuarioRepository,
        IGlowTokenService tokenService,
        IMensagemNotificacaoService mensagemNotificacaoService,
        IOptions<MensageriaWhatsAppOptions> whatsAppOptions,
        IOptions<AuthOptions> authOptions)
    {
        _usuarioRepository = usuarioRepository;
        _tokenService = tokenService;
        _mensagemNotificacaoService = mensagemNotificacaoService;
        _whatsAppOptions = whatsAppOptions.Value;
        _authOptions = authOptions.Value;
    }

    public async Task<WhatsAppConfirmacaoInstrucoesDto> IniciarConfirmacaoAsync(
        Usuario usuario,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(usuario.Telefone))
        {
            throw new ConfirmacaoWhatsAppInvalidaException();
        }

        var tokenPlano = _tokenService.GerarRefreshToken();
        var codigoPlano = GerarCodigoNumerico(_authOptions.ConfirmacaoCodigoDigitos);

        usuario.WhatsAppConfirmacaoTokenHash = _tokenService.HashToken(tokenPlano);
        usuario.WhatsAppConfirmacaoCodigoHash = _tokenService.HashToken(codigoPlano);
        usuario.WhatsAppConfirmacaoExpiraEm = DateTime.UtcNow.AddHours(_authOptions.ConfirmacaoWhatsAppHoras);
        usuario.UpdatedAt = DateTime.UtcNow;

        _usuarioRepository.Atualizar(usuario);
        await _usuarioRepository.SalvarAlteracoesAsync(cancellationToken);

        var instrucoes = ConfirmacaoWhatsAppInstrucoesBuilder.Criar(
            _whatsAppOptions,
            codigoPlano,
            usuario.WhatsAppConfirmacaoExpiraEm.Value,
            emailEnviado: false);

        var emailEnviado = await EnviarEmailsConfirmacaoAsync(
            [usuario.Email],
            usuario.Nome,
            usuario.Telefone,
            instrucoes,
            cancellationToken);

        instrucoes.EmailEnviado = emailEnviado;
        return instrucoes;
    }

    public async Task ConfirmarPorTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ConfirmacaoWhatsAppInvalidaException();
        }

        var hash = _tokenService.HashToken(token.Trim());
        var usuario = await _usuarioRepository.ObterPorWhatsAppConfirmacaoTokenHashAsync(hash, cancellationToken);
        await ConfirmarUsuarioAsync(usuario, cancellationToken);
    }

    public async Task ConfirmarPorCodigoAsync(
        string telefone,
        string codigo,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(telefone) || string.IsNullOrWhiteSpace(codigo))
        {
            throw new ConfirmacaoWhatsAppInvalidaException();
        }

        var telefoneNormalizado = TelefoneHelper.NormalizarParaWhatsApp(telefone);
        var hash = _tokenService.HashToken(codigo.Trim());
        var usuario = await _usuarioRepository.ObterPorWhatsAppConfirmacaoCodigoHashAsync(hash, cancellationToken);

        if (usuario is null || !TelefoneHelper.SaoEquivalentes(usuario.Telefone, telefoneNormalizado))
        {
            throw new ConfirmacaoWhatsAppInvalidaException();
        }

        await ConfirmarUsuarioAsync(usuario, cancellationToken);
    }

    public async Task<WhatsAppConfirmacaoInboundResultado> TentarConfirmarPorMensagemInboundAsync(
        string telefoneRemetente,
        string textoMensagem,
        CancellationToken cancellationToken = default)
    {
        var telefoneNormalizado = TelefoneHelper.NormalizarParaWhatsApp(telefoneRemetente);
        if (string.IsNullOrWhiteSpace(telefoneNormalizado))
        {
            return WhatsAppConfirmacaoInboundResultado.Ignorado(WhatsAppConfirmacaoInboundMotivoIgnorado.TelefoneInvalido);
        }

        var usuario = await _usuarioRepository.ObterPorTelefoneNormalizadoAsync(telefoneNormalizado, cancellationToken);
        if (usuario is null)
        {
            return WhatsAppConfirmacaoInboundResultado.Ignorado(WhatsAppConfirmacaoInboundMotivoIgnorado.EntidadeNaoEncontrada);
        }

        if (usuario.WhatsAppConfirmadoEm.HasValue)
        {
            return WhatsAppConfirmacaoInboundResultado.Ignorado(WhatsAppConfirmacaoInboundMotivoIgnorado.JaConfirmado);
        }

        if (!usuario.PendenteConfirmacaoWhatsApp())
        {
            return WhatsAppConfirmacaoInboundResultado.Ignorado(WhatsAppConfirmacaoInboundMotivoIgnorado.SemPendencia);
        }

        if (!ConfirmacaoWhatsAppCodigoHelper.MensagemContemCodigoValido(
                textoMensagem,
                usuario.WhatsAppConfirmacaoCodigoHash,
                _authOptions.ConfirmacaoCodigoDigitos,
                _tokenService))
        {
            return WhatsAppConfirmacaoInboundResultado.Ignorado(WhatsAppConfirmacaoInboundMotivoIgnorado.CodigoInvalido);
        }

        await ConfirmarUsuarioAsync(usuario, cancellationToken);

        return WhatsAppConfirmacaoInboundResultado.SucessoUsuario(
            usuario.Nome,
            telefoneNormalizado);
    }

    public async Task ReenviarConfirmacaoAsync(string email, CancellationToken cancellationToken = default)
    {
        var emailNormalizado = ConfirmacaoEmailService.NormalizarEmail(email);
        if (string.IsNullOrEmpty(emailNormalizado))
        {
            return;
        }

        var usuario = await _usuarioRepository.ObterPorEmailAsync(emailNormalizado, cancellationToken);
        if (usuario is null || usuario.WhatsAppConfirmadoEm.HasValue)
        {
            return;
        }

        await IniciarConfirmacaoAsync(usuario, cancellationToken);
    }

    private async Task<bool> EnviarEmailsConfirmacaoAsync(
        IReadOnlyList<string> emailsDestino,
        string nomeDestinatario,
        string telefonePerfil,
        WhatsAppConfirmacaoInstrucoesDto instrucoes,
        CancellationToken cancellationToken)
    {
        var destinatarios = EmailDestinoHelper.Deduplicar(emailsDestino);
        if (destinatarios.Count == 0 || string.IsNullOrWhiteSpace(instrucoes.LinkWhatsApp))
        {
            return false;
        }

        var conteudo = ConfirmacaoWhatsAppEmailTemplate.Criar(
            nomeDestinatario,
            telefonePerfil,
            instrucoes.LinkWhatsApp,
            instrucoes.CodigoConfirmacao,
            instrucoes.MensagemSugerida,
            _authOptions.ConfirmacaoWhatsAppHoras);

        foreach (var destinatario in destinatarios)
        {
            await _mensagemNotificacaoService.RegistrarAsync(new RegistrarMensagemNotificacaoDto
            {
                Canal = CanalMensagemNotificacao.Email,
                Destinatario = destinatario,
                Assunto = "Confirme seu WhatsApp no Glow Up Connect",
                Conteudo = conteudo,
                Prioridade = 2
            }, cancellationToken);
        }

        return true;
    }

    private async Task ConfirmarUsuarioAsync(Usuario? usuario, CancellationToken cancellationToken)
    {
        if (usuario is null
            || usuario.WhatsAppConfirmacaoExpiraEm is null
            || usuario.WhatsAppConfirmacaoExpiraEm <= DateTime.UtcNow)
        {
            throw new ConfirmacaoWhatsAppInvalidaException();
        }

        if (usuario.WhatsAppConfirmadoEm.HasValue)
        {
            return;
        }

        usuario.WhatsAppConfirmadoEm = DateTime.UtcNow;
        usuario.WhatsAppOptIn = true;
        usuario.LimparConfirmacaoWhatsApp();
        usuario.UpdatedAt = DateTime.UtcNow;

        _usuarioRepository.Atualizar(usuario);
        await _usuarioRepository.SalvarAlteracoesAsync(cancellationToken);
    }

    private static string GerarCodigoNumerico(int digitos)
    {
        var max = (int)Math.Pow(10, digitos);
        var valor = RandomNumberGenerator.GetInt32(0, max);
        return valor.ToString($"D{digitos}");
    }
}
