using GLOWAPI.API.Models;
using GLOWAPI.Application.DTOs.Pagamentos;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Options;
using GLOWAPI.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

    public WebhooksPagamentoController(
        IWebhookPagamentoService webhookPagamentoService,
        IMercadoPagoWebhookSignatureValidator signatureValidator,
        IOptions<MercadoPagoOptions> mercadoPagoOptions,
        IOptions<WebhookPagamentoOptions> webhookPagamentoOptions,
        IWebHostEnvironment environment)
    {
        _webhookPagamentoService = webhookPagamentoService;
        _signatureValidator = signatureValidator;
        _mercadoPagoOptions = mercadoPagoOptions.Value;
        _webhookPagamentoOptions = webhookPagamentoOptions.Value;
        _environment = environment;
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
        [FromBody] JsonElement payload,
        [FromQuery(Name = "id")] string? id,
        [FromQuery(Name = "topic")] string? topic,
        [FromQuery(Name = "type")] string? type,
        CancellationToken cancellationToken)
    {
        var rawPayload = payload.GetRawText();

        var dataIdQuery = Request.Query["data.id"].FirstOrDefault()
            ?? Request.Query["data_id"].FirstOrDefault();

        if (DeveValidarAssinaturaMercadoPago()
            && !_signatureValidator.Validar(
                Request.Headers["x-signature"].FirstOrDefault(),
                Request.Headers["x-request-id"].FirstOrDefault(),
                dataIdQuery,
                rawPayload,
                out var motivoFalha))
        {
            return Unauthorized(ApiErrorResponse.From(
                "Webhook do Mercado Pago nao autorizado.",
                motivoFalha ?? "WEBHOOK_SIGNATURE_INVALID"));
        }

        var eventId = ExtrairString(payload, "id")
            ?? ExtrairString(payload, "data.id")
            ?? id
            ?? Guid.NewGuid().ToString("N");
        var eventType = ExtrairString(payload, "action")
            ?? ExtrairString(payload, "type")
            ?? type
            ?? topic
            ?? "payment.updated";

        var webhook = await _webhookPagamentoService.RegistrarAsync(new RegistrarWebhookPagamentoRequestDto
        {
            Gateway = GatewayPagamento.MercadoPago,
            EventId = eventId,
            EventType = eventType,
            Payload = rawPayload
        }, cancellationToken);

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

    private bool DeveValidarAssinaturaMercadoPago() =>
        !string.IsNullOrWhiteSpace(_mercadoPagoOptions.WebhookSecret)
        || _environment.IsStaging()
        || _environment.IsProduction();

    private static string? ExtrairString(JsonElement root, string path)
    {
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
}
