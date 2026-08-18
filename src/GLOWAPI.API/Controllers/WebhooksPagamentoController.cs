using GLOWAPI.API.Models;
using GLOWAPI.Application.DTOs.Pagamentos;
using GLOWAPI.Application.Helpers;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Options;
using GLOWAPI.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace GLOWAPI.API.Controllers;

[ApiController]
[Route("api/webhooks/pagamentos")]
public class WebhooksPagamentoController : ControllerBase
{
    private readonly IWebhookPagamentoService _webhookPagamentoService;
    private readonly IMercadoPagoWebhookSignatureValidator _signatureValidator;
    private readonly MercadoPagoOptions _mercadoPagoOptions;
    private readonly WebhookPagamentoOptions _webhookPagamentoOptions;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<WebhooksPagamentoController> _logger;

    public WebhooksPagamentoController(
        IWebhookPagamentoService webhookPagamentoService,
        IMercadoPagoWebhookSignatureValidator signatureValidator,
        IOptions<MercadoPagoOptions> mercadoPagoOptions,
        IOptions<WebhookPagamentoOptions> webhookPagamentoOptions,
        IWebHostEnvironment environment,
        ILogger<WebhooksPagamentoController> logger)
    {
        _webhookPagamentoService = webhookPagamentoService;
        _signatureValidator = signatureValidator;
        _mercadoPagoOptions = mercadoPagoOptions.Value;
        _webhookPagamentoOptions = webhookPagamentoOptions.Value;
        _environment = environment;
        _logger = logger;
    }

    /// <summary>
    /// Entrada genérica (ex.: testes / gateways internos). Em produção exige
    /// <c>X-Glow-Webhook-Secret</c>; o caminho oficial do Mercado Pago é
    /// <c>/mercado-pago</c> com validação de assinatura.
    /// </summary>
    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Registrar(
        [FromBody] RegistrarWebhookPagamentoRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!AutorizarWebhookGenerico())
        {
            return NotFound();
        }

        var webhook = await _webhookPagamentoService.RegistrarAsync(request, cancellationToken);
        return Ok(ApiSuccessResponse<WebhookPagamentoResponseDto>.From(
            webhook.Duplicado
                ? "Webhook de pagamento ja registrado."
                : "Webhook de pagamento registrado com sucesso.",
            webhook));
    }

    [AllowAnonymous]
    [HttpPost("mercado-pago")]
    [HttpPost("mercadopago")]
    public async Task<IActionResult> RegistrarMercadoPago(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] JsonElement payload,
        [FromQuery(Name = "id")] string? id,
        [FromQuery(Name = "topic")] string? topic,
        [FromQuery(Name = "type")] string? type,
        CancellationToken cancellationToken)
    {
        var rawPayload = payload.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null
            ? "{}"
            : payload.GetRawText();

        var dataIdQuery = ObterDataIdDaQuery();
        var signatureHeader = Request.Headers["x-signature"].FirstOrDefault();
        var requestIdHeader = Request.Headers["x-request-id"].FirstOrDefault();
        var userAgent = Request.Headers.UserAgent.ToString();
        var temIdentificadorQuery = !string.IsNullOrWhiteSpace(dataIdQuery)
            || !string.IsNullOrWhiteSpace(id)
            || !string.IsNullOrWhiteSpace(topic);

        var ipnSemAssinatura = MercadoPagoWebhookIpn.EhSemAssinatura(
            signatureHeader,
            userAgent,
            temIdentificadorQuery);
        var secretConfigurado = !string.IsNullOrWhiteSpace(_mercadoPagoOptions.WebhookSecret);
        var liveMode = ExtrairLiveMode(payload);

        _logger.LogInformation(
            "Webhook Mercado Pago recebido. Path={Path} Query={Query} UserAgent={UserAgent} HasXSignature={HasXSignature} HasXRequestId={HasXRequestId} SignatureTs={SignatureTs} DataIdQuery={DataIdQuery} Id={Id} Topic={Topic} Type={Type} IpnSemAssinatura={IpnSemAssinatura} ValidarHmac={ValidarHmac} SecretConfigurado={SecretConfigurado} SecretLength={SecretLength} ContentLength={ContentLength} LiveMode={LiveMode}",
            Request.Path.Value,
            Request.QueryString.Value,
            Truncar(userAgent, 200),
            !string.IsNullOrWhiteSpace(signatureHeader),
            !string.IsNullOrWhiteSpace(requestIdHeader),
            MercadoPagoWebhookIpn.ExtrairTimestampAssinatura(signatureHeader),
            dataIdQuery,
            id,
            topic,
            type,
            ipnSemAssinatura,
            DeveValidarAssinaturaMercadoPago() && !ipnSemAssinatura,
            secretConfigurado,
            secretConfigurado ? _mercadoPagoOptions.WebhookSecret.Trim().Trim('"').Length : 0,
            Request.ContentLength,
            liveMode);

        if (DeveValidarAssinaturaMercadoPago()
            && !ipnSemAssinatura
            && !_signatureValidator.Validar(
                signatureHeader,
                requestIdHeader,
                dataIdQuery,
                rawPayload,
                out var motivoFalha))
        {
            _logger.LogWarning(
                "Webhook Mercado Pago recusado. Motivo={Motivo} Path={Path} Query={Query} UserAgent={UserAgent} HasXSignature={HasXSignature} SignatureTs={SignatureTs} DataIdQuery={DataIdQuery} LiveMode={LiveMode}",
                motivoFalha,
                Request.Path.Value,
                Request.QueryString.Value,
                Truncar(userAgent, 200),
                !string.IsNullOrWhiteSpace(signatureHeader),
                MercadoPagoWebhookIpn.ExtrairTimestampAssinatura(signatureHeader),
                dataIdQuery,
                liveMode);
            return Unauthorized(ApiErrorResponse.From(
                "Webhook do Mercado Pago nao autorizado.",
                motivoFalha ?? "WEBHOOK_SIGNATURE_INVALID"));
        }

        var eventId = ExtrairString(payload, "id")
            ?? ExtrairString(payload, "data.id")
            ?? dataIdQuery
            ?? id
            ?? Guid.NewGuid().ToString("N");
        var eventType = ExtrairString(payload, "action")
            ?? ExtrairString(payload, "type")
            ?? type
            ?? topic
            ?? "payment.updated";

        if (string.Equals(rawPayload, "{}", StringComparison.Ordinal)
            && !string.IsNullOrWhiteSpace(eventId))
        {
            rawPayload = JsonSerializer.Serialize(new { data = new { id = eventId }, type = eventType });
        }

        var webhook = await _webhookPagamentoService.RegistrarAsync(new RegistrarWebhookPagamentoRequestDto
        {
            Gateway = GatewayPagamento.MercadoPago,
            EventId = eventId,
            EventType = eventType,
            Payload = rawPayload
        }, cancellationToken);

        _logger.LogInformation(
            "Webhook Mercado Pago aceito. WebhookId={WebhookId} EventId={EventId} EventType={EventType} Processado={Processado} Duplicado={Duplicado} IpnSemAssinatura={IpnSemAssinatura}",
            webhook.Id,
            webhook.EventId,
            webhook.EventType,
            webhook.Processado,
            webhook.Duplicado,
            ipnSemAssinatura);

        return Ok(ApiSuccessResponse<WebhookPagamentoResponseDto>.From(
            webhook.Duplicado
                ? "Webhook do Mercado Pago ja registrado."
                : "Webhook do Mercado Pago registrado com sucesso.",
            webhook));
    }

    private bool AutorizarWebhookGenerico()
    {
        var expected = _webhookPagamentoOptions.RegistrarSecret?.Trim() ?? string.Empty;

        if (!string.IsNullOrEmpty(expected))
        {
            return Request.Headers.TryGetValue(WebhookPagamentoOptions.SecretHeaderName, out var provided)
                && string.Equals(provided.ToString(), expected, StringComparison.Ordinal);
        }

        // Sem segredo configurado: desabilita em Production/Staging; libera só em ambientes locais/teste.
        return _environment.IsDevelopment() || _environment.IsEnvironment("Testing");
    }

    private string? ObterDataIdDaQuery()
    {
        foreach (var key in new[] { "data.id", "data_id" })
        {
            var valor = Request.Query[key].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(valor))
            {
                return valor;
            }
        }

        return Request.Query
            .FirstOrDefault(par => par.Key.Replace("_", ".", StringComparison.OrdinalIgnoreCase) == "data.id")
            .Value
            .FirstOrDefault();
    }

    private bool DeveValidarAssinaturaMercadoPago() =>
        !string.IsNullOrWhiteSpace(_mercadoPagoOptions.WebhookSecret)
        || _environment.IsStaging()
        || _environment.IsProduction();

    private static string? ExtrairString(JsonElement root, string path)
    {
        if (root.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            return null;
        }

        var current = root;
        foreach (var segment in path.Split('.'))
        {
            if (current.ValueKind != JsonValueKind.Object
                || !current.TryGetProperty(segment, out current))
            {
                return null;
            }
        }

        return current.ValueKind switch
        {
            JsonValueKind.String => current.GetString(),
            JsonValueKind.Number => current.GetRawText(),
            _ => null
        };
    }

    private static string? ExtrairLiveMode(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object
            || !root.TryGetProperty("live_mode", out var liveMode))
        {
            return null;
        }

        return liveMode.ValueKind switch
        {
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.String => liveMode.GetString(),
            _ => liveMode.GetRawText()
        };
    }

    private static string Truncar(string? valor, int maximo)
    {
        if (string.IsNullOrEmpty(valor) || valor.Length <= maximo)
        {
            return valor ?? string.Empty;
        }

        return valor[..maximo];
    }
}
