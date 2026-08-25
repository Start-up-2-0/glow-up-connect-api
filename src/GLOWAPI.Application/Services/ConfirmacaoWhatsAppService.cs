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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GLOWAPI.Application.Services;

public class ConfirmacaoWhatsAppService : IConfirmacaoWhatsAppService
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IGlowTokenService _tokenService;
    private readonly IMensagemNotificacaoService _mensagemNotificacaoService;
    private readonly MensageriaWhatsAppOptions _whatsAppOptions;
    private readonly AuthOptions _authOptions;
    private readonly ILogger<ConfirmacaoWhatsAppService> _logger;

    public ConfirmacaoWhatsAppService(
        IUsuarioRepository usuarioRepository,
        IGlowTokenService tokenService,
        IMensagemNotificacaoService mensagemNotificacaoService,
        IOptions<MensageriaWhatsAppOptions> whatsAppOptions,
        IOptions<AuthOptions> authOptions,
        ILogger<ConfirmacaoWhatsAppService> logger)
    {
        _usuarioRepository = usuarioRepository;
        _tokenService = tokenService;
        _mensagemNotificacaoService = mensagemNotificacaoService;
        _whatsAppOptions = whatsAppOptions.Value;
        _authOptions = authOptions.Value;
        _logger = logger;
    }

    public async Task<WhatsAppConfirmacaoInstrucoesDto> IniciarConfirmacaoAsync(
        Usuario usuario,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(usuario.Telefone))
        {
            throw new ConfirmacaoWhatsAppInvalidaException();
        }

        var tokenConfirmacao = ConfirmacaoWhatsAppTokenHelper.Gerar(
            usuario.Id, ConfirmacaoWhatsAppTokenHelper.TipoConta, usuario.Telefone);

        usuario.WhatsAppConfirmacaoTokenHash = _tokenService.HashToken(tokenConfirmacao);
        usuario.WhatsAppConfirmacaoCodigoHash = null;
        usuario.WhatsAppConfirmacaoExpiraEm = null;
        usuario.UpdatedAt = DateTime.UtcNow;

        _usuarioRepository.Atualizar(usuario);
        await _usuarioRepository.SalvarAlteracoesAsync(cancellationToken);

        var instrucoes = ConfirmacaoWhatsAppInstrucoesBuilder.Criar(
            _whatsAppOptions,
            _authOptions,
            usuario.Telefone,
            tokenConfirmacao,
            whatsAppEnviado: false,
            emailEnviado: false);

        instrucoes.WhatsAppEnviado = await EnviarWhatsAppConfirmacaoAsync(
            usuario.Telefone,
            instrucoes.LinkConfirmacao,
            cancellationToken);

        instrucoes.EmailEnviado = await EnviarEmailsConfirmacaoAsync(
            [usuario.Email],
            usuario.Nome,
            usuario.Telefone,
            instrucoes,
            cancellationToken);

        return instrucoes;
    }

    public Task ConfirmarPorTokenAsync(string token, CancellationToken cancellationToken = default) =>
        throw new ConfirmacaoWhatsAppInvalidaException();

    public Task ConfirmarPorCodigoAsync(
        string telefone,
        string codigo,
        CancellationToken cancellationToken = default) =>
        throw new ConfirmacaoWhatsAppInvalidaException();

    public async Task<WhatsAppConfirmacaoInboundResultado> TentarConfirmarPorMensagemInboundAsync(
        string telefoneRemetente,
        string textoMensagem,
        CancellationToken cancellationToken = default)
    {
        var resultadoPorCodigo = await TentarConfirmarPorCodigoLegadoAsync(
            telefoneRemetente,
            textoMensagem,
            cancellationToken);

        if (resultadoPorCodigo is not null)
        {
            return resultadoPorCodigo;
        }

        var usuarioPorToken = await BuscarUsuarioPorTokenNaMensagemAsync(textoMensagem, cancellationToken);
        if (usuarioPorToken is not null)
        {
            return await ProcessarConfirmacaoInboundDoUsuarioAsync(usuarioPorToken, cancellationToken);
        }

        if (ConfirmacaoWhatsAppTokenHelper.PareceTentativaConfirmacaoPorToken(textoMensagem))
        {
            return WhatsAppConfirmacaoInboundResultado.Ignorado(WhatsAppConfirmacaoInboundMotivoIgnorado.EntidadeNaoEncontrada);
        }

        if (!ConfirmacaoWhatsAppTokenHelper.MensagemContemTokenConfirmacao(textoMensagem, telefoneRemetente))
        {
            return WhatsAppConfirmacaoInboundResultado.Ignorado(WhatsAppConfirmacaoInboundMotivoIgnorado.CodigoInvalido);
        }

        var usuario = await BuscarUsuarioPorTelefoneRemetenteAsync(telefoneRemetente, cancellationToken);
        if (usuario is null)
        {
            return WhatsAppConfirmacaoInboundResultado.Ignorado(WhatsAppConfirmacaoInboundMotivoIgnorado.EntidadeNaoEncontrada);
        }

        if (!TelefoneHelper.SaoEquivalentes(usuario.Telefone, telefoneRemetente))
        {
            return WhatsAppConfirmacaoInboundResultado.Ignorado(
                WhatsAppConfirmacaoInboundMotivoIgnorado.CodigoInvalido,
                usuario.Nome,
                ObterTelefoneCadastradoNormalizado(usuario),
                usuario.Email,
                usuarioId: usuario.Id);
        }

        return await ProcessarConfirmacaoInboundDoUsuarioAsync(usuario, cancellationToken);
    }

    public async Task<WhatsAppConfirmacaoInboundRespostaDestino?> ResolverDestinoRespostaInboundAsync(
        string telefoneRemetente,
        string textoMensagem,
        CancellationToken cancellationToken = default)
    {
        var destinoPorCodigo = await ResolverDestinoPorCodigoAsync(textoMensagem, cancellationToken);
        if (destinoPorCodigo is not null)
        {
            return destinoPorCodigo;
        }

        var usuarioPorToken = await BuscarUsuarioPorTokenNaMensagemAsync(textoMensagem, cancellationToken);
        if (usuarioPorToken is not null)
        {
            return new WhatsAppConfirmacaoInboundRespostaDestino(
                ObterTelefoneCadastradoNormalizado(usuarioPorToken),
                usuarioPorToken.Nome,
                UsuarioId: usuarioPorToken.Id);
        }

        if (ConfirmacaoWhatsAppTokenHelper.PareceTentativaConfirmacaoPorToken(textoMensagem)) return null;

        if (!ConfirmacaoWhatsAppTokenHelper.MensagemContemTokenConfirmacao(textoMensagem, telefoneRemetente))
        {
            return null;
        }

        var usuario = await BuscarUsuarioPorTelefoneRemetenteAsync(telefoneRemetente, cancellationToken);
        if (usuario is null)
        {
            var telefoneNormalizado = TelefoneHelper.NormalizarParaWhatsApp(telefoneRemetente);
            if (string.IsNullOrWhiteSpace(telefoneNormalizado))
            {
                return null;
            }

            return new WhatsAppConfirmacaoInboundRespostaDestino(telefoneNormalizado, null);
        }

        return new WhatsAppConfirmacaoInboundRespostaDestino(
            ObterTelefoneCadastradoNormalizado(usuario),
            usuario.Nome,
            UsuarioId: usuario.Id);
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

    private async Task<bool> EnviarWhatsAppConfirmacaoAsync(
        string telefoneDestino,
        string linkConfirmacao,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(telefoneDestino) || string.IsNullOrWhiteSpace(linkConfirmacao))
        {
            return false;
        }

        var conteudo = ConfirmacaoWhatsAppTemplate.MensagemOutboundConfirmacao(linkConfirmacao);

        await _mensagemNotificacaoService.RegistrarAsync(new RegistrarMensagemNotificacaoDto
        {
            Canal = CanalMensagemNotificacao.WhatsApp,
            Destinatario = TelefoneHelper.NormalizarParaWhatsApp(telefoneDestino),
            Assunto = "Confirmacao WhatsApp Glow Up Connect",
            Conteudo = conteudo,
            EhVerificacaoWhatsApp = true,
            Prioridade = 2
        }, cancellationToken);

        return true;
    }

    private async Task<bool> EnviarEmailsConfirmacaoAsync(
        IReadOnlyList<string> emailsDestino,
        string nomeDestinatario,
        string telefonePerfil,
        WhatsAppConfirmacaoInstrucoesDto instrucoes,
        CancellationToken cancellationToken)
    {
        var destinatarios = EmailDestinoHelper.Deduplicar(emailsDestino);
        if (destinatarios.Count == 0
            || string.IsNullOrWhiteSpace(instrucoes.LinkConfirmacao)
            || string.IsNullOrWhiteSpace(instrucoes.LinkWhatsApp))
        {
            return false;
        }

        var conteudo = ConfirmacaoWhatsAppEmailTemplate.Criar(
            nomeDestinatario,
            telefonePerfil,
            instrucoes.LinkConfirmacao,
            instrucoes.LinkWhatsApp);

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

    private async Task<WhatsAppConfirmacaoInboundRespostaDestino?> ResolverDestinoPorCodigoAsync(
        string textoMensagem,
        CancellationToken cancellationToken)
    {
        var usuario = await BuscarUsuarioPorCodigoNaMensagemAsync(textoMensagem, cancellationToken);
        if (usuario is null)
        {
            return null;
        }

        var telefoneCadastrado = ObterTelefoneCadastradoNormalizado(usuario);
        if (string.IsNullOrWhiteSpace(telefoneCadastrado))
        {
            return null;
        }

        LogarDonoCodigoIdentificadoNoBanco(usuario, telefoneCadastrado, "resolver_destino");

        return new WhatsAppConfirmacaoInboundRespostaDestino(
            telefoneCadastrado,
            usuario.Nome,
            UsuarioId: usuario.Id);
    }

    private void LogarDonoCodigoIdentificadoNoBanco(Usuario usuario, string telefoneCadastrado, string origem)
    {
        _logger.LogInformation(
            "Confirmacao WhatsApp inbound: dono do codigo identificado no banco ({Origem}). UsuarioId={UsuarioId}, Nome={Nome}, TelefoneCadastrado={TelefoneCadastrado}",
            origem,
            usuario.Id,
            usuario.Nome,
            telefoneCadastrado);
    }

    private async Task<WhatsAppConfirmacaoInboundResultado?> TentarConfirmarPorCodigoLegadoAsync(
        string telefoneRemetente,
        string textoMensagem,
        CancellationToken cancellationToken)
    {
        if (!ConfirmacaoWhatsAppCodigoHelper.PareceTentativaConfirmacaoPorCodigo(textoMensagem))
        {
            return null;
        }

        var usuarioPorCodigo = await BuscarUsuarioPorCodigoNaMensagemAsync(textoMensagem, cancellationToken);
        if (usuarioPorCodigo is not null)
        {
            LogarDonoCodigoIdentificadoNoBanco(
                usuarioPorCodigo,
                ObterTelefoneCadastradoNormalizado(usuarioPorCodigo),
                "confirmar_mensagem");

            return await ProcessarConfirmacaoInboundDoUsuarioPorCodigoAsync(
                usuarioPorCodigo,
                textoMensagem,
                cancellationToken);
        }

        var telefoneNormalizado = TelefoneHelper.NormalizarParaWhatsApp(telefoneRemetente);
        if (string.IsNullOrWhiteSpace(telefoneNormalizado))
        {
            return WhatsAppConfirmacaoInboundResultado.Ignorado(WhatsAppConfirmacaoInboundMotivoIgnorado.CodigoInvalido);
        }

        var usuario = await _usuarioRepository.ObterPorTelefoneNormalizadoAsync(telefoneNormalizado, cancellationToken);
        if (usuario is null)
        {
            return WhatsAppConfirmacaoInboundResultado.Ignorado(WhatsAppConfirmacaoInboundMotivoIgnorado.EntidadeNaoEncontrada);
        }

        return await ProcessarConfirmacaoInboundDoUsuarioPorCodigoAsync(usuario, textoMensagem, cancellationToken);
    }

    private async Task<Usuario?> BuscarUsuarioPorTelefoneRemetenteAsync(
        string telefoneRemetente,
        CancellationToken cancellationToken)
    {
        var telefoneNormalizado = TelefoneHelper.NormalizarParaWhatsApp(telefoneRemetente);
        if (!string.IsNullOrWhiteSpace(telefoneNormalizado))
        {
            var usuario = await _usuarioRepository.ObterPorTelefoneNormalizadoAsync(telefoneNormalizado, cancellationToken);
            if (usuario is not null)
            {
                return usuario;
            }
        }

        var telefoneInbound = TelefoneHelper.NormalizarParaConfirmacaoInbound(telefoneRemetente);
        if (string.IsNullOrWhiteSpace(telefoneInbound))
        {
            return null;
        }

        return await _usuarioRepository.ObterPorTelefoneNormalizadoAsync(telefoneInbound, cancellationToken);
    }

    private async Task<Usuario?> BuscarUsuarioPorTokenNaMensagemAsync(
        string textoMensagem,
        CancellationToken cancellationToken)
    {
        foreach (var token in ConfirmacaoWhatsAppTokenHelper.ExtrairTokensCandidatos(textoMensagem))
        {
            if (!ConfirmacaoWhatsAppTokenHelper.TentarDecodificar(token, out var payload)
                || payload!.Type != ConfirmacaoWhatsAppTokenHelper.TipoConta)
            {
                continue;
            }

            var hash = _tokenService.HashToken(token);
            var usuario = await _usuarioRepository.ObterPorIdAsync(payload.Id, cancellationToken);
            if (usuario is null || usuario.WhatsAppConfirmacaoTokenHash != hash)
            {
                continue;
            }

            if (!TelefoneHelper.SaoEquivalentes(payload.Phone, usuario.Telefone))
            {
                continue;
            }

            return usuario;
        }

        return null;
    }

    private async Task<Usuario?> BuscarUsuarioPorCodigoNaMensagemAsync(
        string textoMensagem,
        CancellationToken cancellationToken)
    {
        foreach (var candidato in ConfirmacaoWhatsAppCodigoHelper.ExtrairCandidatosCodigo(
                     textoMensagem,
                     _authOptions.ConfirmacaoCodigoDigitos))
        {
            var hash = _tokenService.HashToken(candidato);
            var usuario = await _usuarioRepository.ObterPorWhatsAppConfirmacaoCodigoHashAsync(hash, cancellationToken);
            if (usuario is not null)
            {
                return usuario;
            }
        }

        return null;
    }

    private async Task<WhatsAppConfirmacaoInboundResultado> ProcessarConfirmacaoInboundDoUsuarioAsync(
        Usuario usuario,
        CancellationToken cancellationToken)
    {
        var telefoneCadastrado = ObterTelefoneCadastradoNormalizado(usuario);

        if (usuario.WhatsAppConfirmadoEm.HasValue)
        {
            return WhatsAppConfirmacaoInboundResultado.Ignorado(
                WhatsAppConfirmacaoInboundMotivoIgnorado.JaConfirmado,
                usuario.Nome,
                telefoneCadastrado,
                usuario.Email,
                usuarioId: usuario.Id);
        }

        if (!usuario.PendenteConfirmacaoWhatsApp())
        {
            return WhatsAppConfirmacaoInboundResultado.Ignorado(
                WhatsAppConfirmacaoInboundMotivoIgnorado.SemPendencia,
                usuario.Nome,
                telefoneCadastrado,
                usuario.Email,
                usuarioId: usuario.Id);
        }

        await ConfirmarUsuarioAsync(usuario, cancellationToken);

        return WhatsAppConfirmacaoInboundResultado.SucessoUsuario(
            usuario.Nome,
            telefoneCadastrado,
            usuario.Id,
            usuario.Email);
    }

    private async Task<WhatsAppConfirmacaoInboundResultado> ProcessarConfirmacaoInboundDoUsuarioPorCodigoAsync(
        Usuario usuario,
        string textoMensagem,
        CancellationToken cancellationToken)
    {
        var telefoneCadastrado = ObterTelefoneCadastradoNormalizado(usuario);

        if (usuario.WhatsAppConfirmadoEm.HasValue)
        {
            return WhatsAppConfirmacaoInboundResultado.Ignorado(
                WhatsAppConfirmacaoInboundMotivoIgnorado.JaConfirmado,
                usuario.Nome,
                telefoneCadastrado,
                usuario.Email,
                usuarioId: usuario.Id);
        }

        if (!usuario.PendenteConfirmacaoWhatsApp())
        {
            return WhatsAppConfirmacaoInboundResultado.Ignorado(
                WhatsAppConfirmacaoInboundMotivoIgnorado.SemPendencia,
                usuario.Nome,
                telefoneCadastrado,
                usuario.Email,
                usuarioId: usuario.Id);
        }

        if (!ConfirmacaoWhatsAppCodigoHelper.MensagemContemCodigoValido(
                textoMensagem,
                usuario.WhatsAppConfirmacaoCodigoHash,
                _authOptions.ConfirmacaoCodigoDigitos,
                _tokenService))
        {
            return WhatsAppConfirmacaoInboundResultado.Ignorado(
                WhatsAppConfirmacaoInboundMotivoIgnorado.CodigoInvalido,
                usuario.Nome,
                telefoneCadastrado,
                usuario.Email,
                usuarioId: usuario.Id);
        }

        await ConfirmarUsuarioAsync(usuario, cancellationToken);

        return WhatsAppConfirmacaoInboundResultado.SucessoUsuario(
            usuario.Nome,
            telefoneCadastrado,
            usuario.Id,
            usuario.Email);
    }

    private static string ObterTelefoneCadastradoNormalizado(Usuario usuario) =>
        TelefoneHelper.NormalizarParaWhatsApp(usuario.Telefone) ?? string.Empty;

    private async Task ConfirmarUsuarioAsync(Usuario? usuario, CancellationToken cancellationToken)
    {
        if (usuario is null)
        {
            throw new ConfirmacaoWhatsAppInvalidaException();
        }

        if (!string.IsNullOrEmpty(usuario.WhatsAppConfirmacaoCodigoHash)
            && (usuario.WhatsAppConfirmacaoExpiraEm is null
                || usuario.WhatsAppConfirmacaoExpiraEm <= DateTime.UtcNow))
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
}
