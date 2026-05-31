using System.Text.Json;
using GLOWAPI.Application.DTOs.Mensageria;
using GLOWAPI.Application.Helpers;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Mensageria;
using GLOWAPI.Application.Options;
using GLOWAPI.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GLOWAPI.Application.Services;

public class WebhookWhatsAppService : IWebhookWhatsAppService
{
    private readonly IConfirmacaoWhatsAppService _confirmacaoWhatsAppService;
    private readonly IConfirmacaoWhatsAppEstabelecimentoService _confirmacaoWhatsAppEstabelecimentoService;
    private readonly IMensagemNotificacaoService _mensagemNotificacaoService;
    private readonly MensageriaWhatsAppOptions _options;
    private readonly ILogger<WebhookWhatsAppService> _logger;

    public WebhookWhatsAppService(
        IConfirmacaoWhatsAppService confirmacaoWhatsAppService,
        IConfirmacaoWhatsAppEstabelecimentoService confirmacaoWhatsAppEstabelecimentoService,
        IMensagemNotificacaoService mensagemNotificacaoService,
        IOptions<MensageriaWhatsAppOptions> options,
        ILogger<WebhookWhatsAppService> logger)
    {
        _confirmacaoWhatsAppService = confirmacaoWhatsAppService;
        _confirmacaoWhatsAppEstabelecimentoService = confirmacaoWhatsAppEstabelecimentoService;
        _mensagemNotificacaoService = mensagemNotificacaoService;
        _options = options.Value;
        _logger = logger;
    }

    public async Task ProcessarEvolutionWebhookAsync(
        JsonElement payload,
        CancellationToken cancellationToken = default)
    {
        if (!EvolutionWebhookParser.IsMensagemRecebida(payload))
        {
            return;
        }

        if (!ValidarApiKey(payload))
        {
            _logger.LogWarning("Webhook WhatsApp ignorado: apikey invalida.");
            return;
        }

        var telefoneRemetente = EvolutionWebhookParser.ExtrairTelefoneRemetente(payload);
        var textoMensagem = EvolutionWebhookParser.ExtrairTextoMensagem(payload);

        if (string.IsNullOrWhiteSpace(telefoneRemetente) || string.IsNullOrWhiteSpace(textoMensagem))
        {
            return;
        }

        var resultadoUsuario = await _confirmacaoWhatsAppService.TentarConfirmarPorMensagemInboundAsync(
            telefoneRemetente,
            textoMensagem,
            cancellationToken);

        if (resultadoUsuario.Confirmado)
        {
            await EnviarRespostaConfirmacaoAsync(resultadoUsuario, cancellationToken);
            return;
        }

        var resultadoEstabelecimento = await _confirmacaoWhatsAppEstabelecimentoService.TentarConfirmarPorMensagemInboundAsync(
            telefoneRemetente,
            textoMensagem,
            cancellationToken);

        if (resultadoEstabelecimento.Confirmado)
        {
            await EnviarRespostaConfirmacaoAsync(resultadoEstabelecimento, cancellationToken);
        }
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

    private async Task EnviarRespostaConfirmacaoAsync(
        Models.Mensageria.WhatsAppConfirmacaoInboundResultado resultado,
        CancellationToken cancellationToken)
    {
        var conteudo = ConfirmacaoWhatsAppTemplate.RespostaConfirmacaoSucesso(resultado.NomeDestinatario);

        await _mensagemNotificacaoService.RegistrarAsync(new RegistrarMensagemNotificacaoDto
        {
            Canal = CanalMensagemNotificacao.WhatsApp,
            Destinatario = resultado.TelefoneResposta,
            Assunto = "WhatsApp confirmado",
            Conteudo = conteudo,
            EstabelecimentoId = resultado.EstabelecimentoId,
            Prioridade = 2
        }, cancellationToken);

        _logger.LogInformation(
            "WhatsApp confirmado via webhook inbound. Tipo={Tipo}, Telefone={Telefone}",
            resultado.Tipo,
            resultado.TelefoneResposta);
    }
}
