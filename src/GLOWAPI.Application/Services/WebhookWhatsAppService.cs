using System.Text.Json;
using GLOWAPI.Application.Helpers;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Models.Mensageria;
using GLOWAPI.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GLOWAPI.Application.Services;

public class WebhookWhatsAppService : IWebhookWhatsAppService
{
    private const int PayloadLogMaxLength = 8000;

    private readonly IConfirmacaoWhatsAppInboundService _confirmacaoWhatsAppInboundService;
    private readonly MensageriaWhatsAppOptions _options;
    private readonly ILogger<WebhookWhatsAppService> _logger;

    public WebhookWhatsAppService(
        IConfirmacaoWhatsAppInboundService confirmacaoWhatsAppInboundService,
        IOptions<MensageriaWhatsAppOptions> options,
        ILogger<WebhookWhatsAppService> logger)
    {
        _confirmacaoWhatsAppInboundService = confirmacaoWhatsAppInboundService;
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

        if (!ConfirmacaoWhatsAppCodigoHelper.PareceTentativaConfirmacao(textoMensagem))
        {
            _logger.LogInformation(
                "Webhook WhatsApp messages-upsert ignorado: mensagem sem tentativa de confirmacao. Evento={Evento}, Instancia={Instancia}, TelefonePresente={TelefonePresente}",
                evento,
                instancia,
                !string.IsNullOrWhiteSpace(telefone));
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

        var contextoConversa = new ConfirmacaoWhatsAppInboundContexto
        {
            RemoteJidConversa = EvolutionWebhookParser.ExtrairRemoteJidConversa(payload),
            RemoteJidAlt = EvolutionWebhookParser.ExtrairRemoteJidAlt(payload)
        };

        await _confirmacaoWhatsAppInboundService.ProcessarAsync(
            telefone,
            textoMensagem,
            contextoConversa,
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
}
