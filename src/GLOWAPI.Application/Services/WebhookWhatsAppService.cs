using System.Text.Json;
using GLOWAPI.Application.DTOs.Mensageria;
using GLOWAPI.Application.Helpers;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Mensageria;
using GLOWAPI.Application.Models.Mensageria;
using GLOWAPI.Application.Options;
using GLOWAPI.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GLOWAPI.Application.Services;

public class WebhookWhatsAppService : IWebhookWhatsAppService
{
    private const int PayloadLogMaxLength = 8000;

    private readonly IConfirmacaoWhatsAppService _confirmacaoWhatsAppService;
    private readonly IConfirmacaoWhatsAppEstabelecimentoService _confirmacaoWhatsAppEstabelecimentoService;
    private readonly IMensagemNotificacaoService _mensagemNotificacaoService;
    private readonly IWhatsAppEnvioImediatoService _envioImediatoService;
    private readonly MensageriaWhatsAppOptions _options;
    private readonly ILogger<WebhookWhatsAppService> _logger;

    public WebhookWhatsAppService(
        IConfirmacaoWhatsAppService confirmacaoWhatsAppService,
        IConfirmacaoWhatsAppEstabelecimentoService confirmacaoWhatsAppEstabelecimentoService,
        IMensagemNotificacaoService mensagemNotificacaoService,
        IWhatsAppEnvioImediatoService envioImediatoService,
        IOptions<MensageriaWhatsAppOptions> options,
        ILogger<WebhookWhatsAppService> logger)
    {
        _confirmacaoWhatsAppService = confirmacaoWhatsAppService;
        _confirmacaoWhatsAppEstabelecimentoService = confirmacaoWhatsAppEstabelecimentoService;
        _mensagemNotificacaoService = mensagemNotificacaoService;
        _envioImediatoService = envioImediatoService;
        _options = options.Value;
        _logger = logger;
    }

    public async Task ProcessarMensagemRecebidaAsync(
        JsonElement payload,
        CancellationToken cancellationToken = default)
    {
        var evento = EvolutionWebhookParser.ExtrairEvento(payload) ?? "(nao informado)";
        var instancia = EvolutionWebhookParser.ExtrairInstancia(payload) ?? "(nao informado)";

        LogPayloadBrutoEvolution("messages-upsert", evento, instancia, payload);
        var fromMe = EvolutionWebhookParser.ExtrairFromMe(payload);
        var diagnostico = EvolutionWebhookParser.ExtrairDiagnosticoRemetente(payload);
        var telefone = diagnostico.TelefoneExtraido;
        var textoMensagem = EvolutionWebhookParser.ExtrairTextoMensagem(payload);

        _logger.LogInformation(
            "Webhook WhatsApp messages-upsert recebido. Evento={Evento}, Instancia={Instancia}, FromMe={FromMe}, TelefonePresente={TelefonePresente}, TextoPresente={TextoPresente}, RemoteJid={RemoteJid}, EhLid={EhLid}, RemoteJidAlt={RemoteJidAltPresente}, SenderPnKey={SenderPnKeyPresente}, SenderPnData={SenderPnDataPresente}, Participant={ParticipantPresente}, SenderInstancia={SenderInstancia}, PushName={PushName}, MotivoTelefoneVazio={MotivoTelefoneVazio}",
            evento,
            instancia,
            fromMe,
            !string.IsNullOrWhiteSpace(telefone),
            !string.IsNullOrWhiteSpace(textoMensagem),
            diagnostico.RemoteJid ?? "(ausente)",
            diagnostico.EhLid,
            diagnostico.RemoteJidAltPresente,
            diagnostico.SenderPnKeyPresente,
            diagnostico.SenderPnDataPresente,
            diagnostico.ParticipantPresente,
            diagnostico.SenderInstancia ?? "(ausente)",
            diagnostico.PushName ?? "(ausente)",
            diagnostico.MotivoTelefoneVazio ?? "(n/a)");

        var podeProcessarConfirmacao = EvolutionWebhookParser.IsMensagemInboundDoUsuario(payload)
            || ConfirmacaoWhatsAppCodigoHelper.PareceTentativaConfirmacao(textoMensagem);

        if (!podeProcessarConfirmacao)
        {
            _logger.LogInformation(
                "Webhook WhatsApp messages-upsert ignorado: nao inbound. Evento={Evento}, Instancia={Instancia}, Motivo={Motivo}",
                evento,
                instancia,
                EvolutionWebhookParser.DescreverMotivoNaoInbound(payload));
            return;
        }

        if (!ValidarApiKey(payload))
        {
            _logger.LogWarning(
                "Webhook WhatsApp messages-upsert ignorado: apikey invalida. Evento={Evento}, Instancia={Instancia}",
                evento,
                instancia);
            return;
        }

        var remoteJidConversa = EvolutionWebhookParser.ExtrairRemoteJidConversa(payload);
        var remoteJidAlt = EvolutionWebhookParser.ExtrairRemoteJidAlt(payload);
        var contextoResposta = EvolutionWebhookParser.ExtrairContextoRespostaInbound(payload);
        await ProcessarConfirmacaoInboundAsync(
            telefone,
            remoteJidConversa,
            remoteJidAlt,
            contextoResposta,
            textoMensagem,
            cancellationToken);
    }

    public Task ProcessarMensagemEnviadaAsync(
        JsonElement payload,
        CancellationToken cancellationToken = default)
    {
        var evento = EvolutionWebhookParser.ExtrairEvento(payload) ?? "(nao informado)";
        var instancia = EvolutionWebhookParser.ExtrairInstancia(payload) ?? "(nao informado)";

        LogPayloadBrutoEvolution("send-message", evento, instancia, payload);

        if (!ValidarApiKey(payload))
        {
            _logger.LogWarning("Webhook WhatsApp send-message ignorado: apikey invalida.");
            return Task.CompletedTask;
        }

        var telefone = EvolutionWebhookParser.ExtrairTelefoneRemetente(payload);
        var textoMensagem = EvolutionWebhookParser.ExtrairTextoMensagem(payload);

        _logger.LogInformation(
            "Webhook WhatsApp send-message recebido. Telefone={Telefone}, TextoPresente={TextoPresente}",
            string.IsNullOrWhiteSpace(telefone) ? "(nao identificado)" : telefone,
            !string.IsNullOrWhiteSpace(textoMensagem));

        return Task.CompletedTask;
    }

    private async Task ProcessarConfirmacaoInboundAsync(
        string telefoneRemetente,
        string? remoteJidConversa,
        string? remoteJidAlt,
        EvolutionWhatsAppContextoResposta contextoResposta,
        string textoMensagem,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(textoMensagem))
        {
            _logger.LogInformation(
                "Webhook WhatsApp messages-upsert ignorado: texto ausente. TelefonePresente={TelefonePresente}",
                !string.IsNullOrWhiteSpace(telefoneRemetente));
            return;
        }

        if (!ConfirmacaoWhatsAppCodigoHelper.PareceTentativaConfirmacao(textoMensagem))
        {
            _logger.LogInformation(
                "Webhook WhatsApp messages-upsert ignorado: mensagem sem token de confirmacao. TelefonePresente={TelefonePresente}",
                !string.IsNullOrWhiteSpace(telefoneRemetente));
            return;
        }

        var destinoResposta = await ResolverDestinoRespostaInboundAsync(
            telefoneRemetente,
            textoMensagem,
            cancellationToken);

        if (destinoResposta is not null && !string.IsNullOrWhiteSpace(destinoResposta.Telefone))
        {
            LogarDestinoRespostaAutomatica(telefoneRemetente, destinoResposta);

            await EnviarMensagemProcessandoAsync(
                destinoResposta.Telefone,
                remoteJidConversa,
                remoteJidAlt,
                contextoResposta,
                destinoResposta.Nome,
                cancellationToken);
        }

        var resultadoUsuario = await _confirmacaoWhatsAppService.TentarConfirmarPorMensagemInboundAsync(
            telefoneRemetente,
            textoMensagem,
            cancellationToken);

        if (resultadoUsuario.Confirmado)
        {
            await EnviarRespostaConfirmacaoSucessoAsync(
                resultadoUsuario,
                ObterDestinatarioRespostaAutomatica(resultadoUsuario),
                remoteJidConversa,
                remoteJidAlt,
                contextoResposta,
                cancellationToken);
            return;
        }

        LogarMotivoIgnorado("usuario", telefoneRemetente, resultadoUsuario.MotivoIgnorado);

        if (resultadoUsuario.MotivoIgnorado == WhatsAppConfirmacaoInboundMotivoIgnorado.JaConfirmado)
        {
            await EnviarRespostaJaConfirmadoAsync(
                resultadoUsuario,
                ObterDestinatarioRespostaAutomatica(resultadoUsuario),
                remoteJidConversa,
                remoteJidAlt,
                contextoResposta,
                cancellationToken);
            return;
        }

        var resultadoEstabelecimento = await _confirmacaoWhatsAppEstabelecimentoService.TentarConfirmarPorMensagemInboundAsync(
            telefoneRemetente,
            textoMensagem,
            cancellationToken);

        if (resultadoEstabelecimento.Confirmado)
        {
            await EnviarRespostaConfirmacaoSucessoAsync(
                resultadoEstabelecimento,
                ObterDestinatarioRespostaAutomatica(resultadoEstabelecimento),
                remoteJidConversa,
                remoteJidAlt,
                contextoResposta,
                cancellationToken);
            return;
        }

        LogarMotivoIgnorado("estabelecimento", telefoneRemetente, resultadoEstabelecimento.MotivoIgnorado);

        if (resultadoEstabelecimento.MotivoIgnorado == WhatsAppConfirmacaoInboundMotivoIgnorado.JaConfirmado)
        {
            await EnviarRespostaJaConfirmadoAsync(
                resultadoEstabelecimento,
                ObterDestinatarioRespostaAutomatica(resultadoEstabelecimento),
                remoteJidConversa,
                remoteJidAlt,
                contextoResposta,
                cancellationToken);
            return;
        }

        var resultadoFalha = SelecionarResultadoFalha(resultadoUsuario, resultadoEstabelecimento);
        await EnviarRespostaConfirmacaoFalhaAsync(
            resultadoFalha,
            ObterDestinatarioRespostaAutomatica(resultadoFalha),
            remoteJidConversa,
            remoteJidAlt,
            contextoResposta,
            cancellationToken);
    }

    private static WhatsAppConfirmacaoInboundResultado SelecionarResultadoFalha(
        WhatsAppConfirmacaoInboundResultado resultadoUsuario,
        WhatsAppConfirmacaoInboundResultado resultadoEstabelecimento)
    {
        if (resultadoUsuario.PossuiContatoIdentificado
            && DeveNotificarFalha(resultadoUsuario.MotivoIgnorado))
        {
            return resultadoUsuario;
        }

        if (resultadoEstabelecimento.PossuiContatoIdentificado
            && DeveNotificarFalha(resultadoEstabelecimento.MotivoIgnorado))
        {
            return resultadoEstabelecimento;
        }

        return resultadoUsuario.MotivoIgnorado != WhatsAppConfirmacaoInboundMotivoIgnorado.Nenhum
            ? resultadoUsuario
            : resultadoEstabelecimento;
    }

    private static bool DeveNotificarFalha(WhatsAppConfirmacaoInboundMotivoIgnorado motivo) =>
        motivo is WhatsAppConfirmacaoInboundMotivoIgnorado.CodigoInvalido
            or WhatsAppConfirmacaoInboundMotivoIgnorado.SemPendencia
            or WhatsAppConfirmacaoInboundMotivoIgnorado.EntidadeNaoEncontrada;

    private void LogarMotivoIgnorado(
        string tipo,
        string telefone,
        WhatsAppConfirmacaoInboundMotivoIgnorado motivo)
    {
        if (motivo == WhatsAppConfirmacaoInboundMotivoIgnorado.Nenhum)
        {
            return;
        }

        _logger.LogInformation(
            "Webhook WhatsApp messages-upsert sem confirmacao. Tipo={Tipo}, Telefone={Telefone}, Motivo={Motivo}",
            tipo,
            telefone,
            motivo);
    }

    private void LogPayloadBrutoEvolution(
        string endpoint,
        string evento,
        string instancia,
        JsonElement payload)
    {
        var raw = payload.GetRawText();
        if (raw.Length > PayloadLogMaxLength)
        {
            raw = raw[..PayloadLogMaxLength] + "...(truncado)";
        }

        _logger.LogInformation(
            "Webhook WhatsApp {Endpoint} payload bruto Evolution. Evento={Evento}, Instancia={Instancia}, Payload={Payload}",
            endpoint,
            evento,
            instancia,
            raw);
    }

    private bool ValidarApiKey(JsonElement payload)
    {
        if (string.IsNullOrWhiteSpace(_options.WebhookApiKey))
        {
            return true;
        }

        if (payload.TryGetProperty("apikey", out var apikeyPayload)
            && string.Equals(apikeyPayload.GetString(), _options.WebhookApiKey, StringComparison.Ordinal))
        {
            return true;
        }

        return string.Equals(_options.ApiKey, _options.WebhookApiKey, StringComparison.Ordinal)
            && payload.TryGetProperty("apikey", out var apikey)
            && string.Equals(apikey.GetString(), _options.ApiKey, StringComparison.Ordinal);
    }

    private async Task<WhatsAppConfirmacaoInboundRespostaDestino?> ResolverDestinoRespostaInboundAsync(
        string telefoneRemetente,
        string textoMensagem,
        CancellationToken cancellationToken)
    {
        var destinoUsuario = await _confirmacaoWhatsAppService.ResolverDestinoRespostaInboundAsync(
            telefoneRemetente,
            textoMensagem,
            cancellationToken);

        if (destinoUsuario is not null)
        {
            return destinoUsuario;
        }

        return await _confirmacaoWhatsAppEstabelecimentoService.ResolverDestinoRespostaInboundAsync(
            telefoneRemetente,
            textoMensagem,
            cancellationToken);
    }

    private void LogarDestinoRespostaAutomatica(
        string telefoneWebhook,
        WhatsAppConfirmacaoInboundRespostaDestino destino)
    {
        var telefoneWebhookNormalizado = TelefoneHelper.NormalizarParaWhatsApp(telefoneWebhook);
        if (string.IsNullOrWhiteSpace(telefoneWebhookNormalizado))
        {
            _logger.LogInformation(
                "Webhook WhatsApp resposta automatica via telefone cadastrado do codigo. UsuarioId={UsuarioId}, EstabelecimentoId={EstabelecimentoId}, Nome={Nome}, Telefone={Telefone}",
                destino.UsuarioId,
                destino.EstabelecimentoId,
                destino.Nome ?? "(nao informado)",
                destino.Telefone);
            return;
        }

        if (!TelefoneHelper.SaoEquivalentes(telefoneWebhookNormalizado, destino.Telefone))
        {
            _logger.LogInformation(
                "Webhook WhatsApp resposta automatica via telefone cadastrado do codigo (telefone do payload ignorado). UsuarioId={UsuarioId}, EstabelecimentoId={EstabelecimentoId}, TelefonePayload={TelefonePayload}, Nome={Nome}, TelefoneDestino={TelefoneDestino}",
                destino.UsuarioId,
                destino.EstabelecimentoId,
                telefoneWebhookNormalizado,
                destino.Nome ?? "(nao informado)",
                destino.Telefone);
        }
    }

    private static string ObterDestinatarioRespostaAutomatica(
        WhatsAppConfirmacaoInboundResultado resultado) =>
        resultado.TelefoneResposta;

    private async Task EnviarMensagemProcessandoAsync(
        string telefoneRemetente,
        string? remoteJidConversa,
        string? remoteJidAlt,
        EvolutionWhatsAppContextoResposta contextoResposta,
        string? nomeDestinatario,
        CancellationToken cancellationToken)
    {
        var conteudo = string.IsNullOrWhiteSpace(nomeDestinatario)
            ? ConfirmacaoWhatsAppTemplate.RespostaProcessandoConfirmacaoGenerica()
            : ConfirmacaoWhatsAppTemplate.RespostaProcessandoConfirmacao(nomeDestinatario);

        await RegistrarWhatsAppAsync(
            telefoneRemetente,
            remoteJidConversa,
            remoteJidAlt,
            contextoResposta,
            "Confirmacao WhatsApp em processamento",
            conteudo,
            estabelecimentoId: null,
            prioridade: 3,
            cancellationToken);
    }

    private async Task EnviarRespostaConfirmacaoSucessoAsync(
        WhatsAppConfirmacaoInboundResultado resultado,
        string destinatario,
        string? remoteJidConversa,
        string? remoteJidAlt,
        EvolutionWhatsAppContextoResposta contextoResposta,
        CancellationToken cancellationToken)
    {
        var conteudo = ConfirmacaoWhatsAppTemplate.RespostaConfirmacaoSucesso(resultado.NomeDestinatario);

        await RegistrarWhatsAppAsync(
            destinatario,
            remoteJidConversa,
            remoteJidAlt,
            contextoResposta,
            "Confirmacao WhatsApp aprovada",
            conteudo,
            resultado.EstabelecimentoId,
            prioridade: 2,
            cancellationToken);

        _logger.LogInformation(
            "WhatsApp confirmado via webhook inbound. Tipo={Tipo}, UsuarioId={UsuarioId}, EstabelecimentoId={EstabelecimentoId}, Destinatario={Destinatario}",
            resultado.Tipo,
            resultado.UsuarioId,
            resultado.EstabelecimentoId,
            destinatario);
    }

    private async Task EnviarRespostaJaConfirmadoAsync(
        WhatsAppConfirmacaoInboundResultado resultado,
        string destinatario,
        string? remoteJidConversa,
        string? remoteJidAlt,
        EvolutionWhatsAppContextoResposta contextoResposta,
        CancellationToken cancellationToken)
    {
        var conteudo = ConfirmacaoWhatsAppTemplate.RespostaConfirmacaoJaRealizada(resultado.NomeDestinatario);

        await RegistrarWhatsAppAsync(
            destinatario,
            remoteJidConversa,
            remoteJidAlt,
            contextoResposta,
            "WhatsApp ja confirmado",
            conteudo,
            resultado.EstabelecimentoId,
            prioridade: 2,
            cancellationToken);
    }

    private async Task EnviarRespostaConfirmacaoFalhaAsync(
        WhatsAppConfirmacaoInboundResultado resultado,
        string destinatario,
        string? remoteJidConversa,
        string? remoteJidAlt,
        EvolutionWhatsAppContextoResposta contextoResposta,
        CancellationToken cancellationToken)
    {
        var telefoneResposta = ObterTelefoneResposta(resultado, destinatario);
        var conteudoWhatsApp = resultado.PossuiContatoIdentificado
            ? ConfirmacaoWhatsAppTemplate.RespostaConfirmacaoFalha(resultado.NomeDestinatario)
            : ConfirmacaoWhatsAppTemplate.RespostaConfirmacaoFalhaGenerica();

        if (!string.IsNullOrWhiteSpace(telefoneResposta))
        {
            await RegistrarWhatsAppAsync(
                telefoneResposta,
                remoteJidConversa,
                remoteJidAlt,
                contextoResposta,
                "Confirmacao WhatsApp nao concluida",
                conteudoWhatsApp,
                resultado.EstabelecimentoId,
                prioridade: 2,
                cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(resultado.EmailDestinatario))
        {
            var conteudoEmail = resultado.PossuiContatoIdentificado
                ? ConfirmacaoWhatsAppEmailTemplate.CriarFalhaConfirmacao(resultado.NomeDestinatario)
                : ConfirmacaoWhatsAppEmailTemplate.CriarFalhaConfirmacaoGenerica();

            await _mensagemNotificacaoService.RegistrarAsync(new RegistrarMensagemNotificacaoDto
            {
                Canal = CanalMensagemNotificacao.Email,
                Destinatario = resultado.EmailDestinatario,
                Assunto = "Nao conseguimos confirmar seu WhatsApp",
                Conteudo = conteudoEmail,
                EstabelecimentoId = resultado.EstabelecimentoId,
                Prioridade = 2
            }, cancellationToken);
        }

        _logger.LogInformation(
            "Confirmacao WhatsApp nao concluida. Motivo={Motivo}, Telefone={Telefone}, EmailEnviado={EmailEnviado}",
            resultado.MotivoIgnorado,
            telefoneResposta,
            !string.IsNullOrWhiteSpace(resultado.EmailDestinatario));
    }

    private async Task RegistrarWhatsAppAsync(
        string telefoneDestino,
        string? remoteJidConversa,
        string? remoteJidAlt,
        EvolutionWhatsAppContextoResposta contextoResposta,
        string assunto,
        string conteudo,
        int? estabelecimentoId,
        int prioridade,
        CancellationToken cancellationToken)
    {
        var telefoneCadastrado = TelefoneHelper.NormalizarParaWhatsApp(telefoneDestino) ?? telefoneDestino;
        var destinoEvolution = EvolutionDestinoHelper.ResolverDestinoOutbound(telefoneCadastrado, remoteJidConversa);
        var payloadJson = EvolutionDestinoHelper.CriarPayloadOutbound(
            telefoneCadastrado,
            remoteJidConversa,
            remoteJidAlt,
            contextoResposta);

        if (string.IsNullOrWhiteSpace(telefoneCadastrado) || string.IsNullOrWhiteSpace(conteudo))
        {
            _logger.LogWarning(
                "WhatsApp outbound ignorado: destinatario ou conteudo ausente. Assunto={Assunto}, TelefoneCadastradoPresente={TelefoneCadastradoPresente}, ConteudoPresente={ConteudoPresente}, RemoteJid={RemoteJid}",
                assunto,
                !string.IsNullOrWhiteSpace(telefoneCadastrado),
                !string.IsNullOrWhiteSpace(conteudo),
                remoteJidConversa ?? "(ausente)");
            return;
        }

        if (EvolutionWebhookParser.EhRemoteJidLid(remoteJidConversa))
        {
            _logger.LogInformation(
                "WhatsApp outbound para telefone cadastrado (thread @lid). Assunto={Assunto}, RemoteJid={RemoteJid}, TelefoneCadastrado={TelefoneCadastrado}, DestinoEvolution={DestinoEvolution}, TemQuoted={TemQuoted}",
                assunto,
                remoteJidConversa,
                telefoneCadastrado,
                destinoEvolution,
                contextoResposta.TemQuoted);
        }

        var dtoMensagem = new RegistrarMensagemNotificacaoDto
        {
            Canal = CanalMensagemNotificacao.WhatsApp,
            Destinatario = telefoneCadastrado,
            Assunto = assunto,
            Conteudo = conteudo,
            PayloadJson = payloadJson,
            EstabelecimentoId = estabelecimentoId,
            Prioridade = prioridade
        };

        var resultadoImediato = await _envioImediatoService.EnviarTextoAsync(
            telefoneCadastrado,
            conteudo,
            cancellationToken,
            remoteJidConversa,
            remoteJidAlt,
            contextoResposta);

        if (resultadoImediato.Sucesso)
        {
            await _mensagemNotificacaoService.RegistrarEnviadoAsync(
                dtoMensagem,
                resultadoImediato.RespostaProvedor ?? "evolution-whatsapp-imediato",
                cancellationToken);

            _logger.LogInformation(
                "WhatsApp enviado imediatamente no webhook. Assunto={Assunto}, TelefoneCadastrado={TelefoneCadastrado}, Provedor={Provedor}",
                assunto,
                telefoneCadastrado,
                resultadoImediato.RespostaProvedor);
            return;
        }

        _logger.LogWarning(
            "WhatsApp imediato falhou; enfileirando retry. Assunto={Assunto}, TelefoneCadastrado={TelefoneCadastrado}, Erro={Erro}",
            assunto,
            telefoneCadastrado,
            resultadoImediato.MensagemErro);

        await _mensagemNotificacaoService.RegistrarAsync(dtoMensagem, cancellationToken);
    }

    private static string ObterTelefoneResposta(
        WhatsAppConfirmacaoInboundResultado resultado,
        string? fallback = null) =>
        !string.IsNullOrWhiteSpace(resultado.TelefoneResposta)
            ? resultado.TelefoneResposta
            : fallback ?? string.Empty;
}
