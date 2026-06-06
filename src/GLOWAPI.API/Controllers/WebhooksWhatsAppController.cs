using GLOWAPI.API.Models;
using GLOWAPI.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace GLOWAPI.API.Controllers;

[ApiController]
[Route("api/webhooks/whatsapp")]
public class WebhooksWhatsAppController : ControllerBase
{
    private readonly IWebhookWhatsAppService _webhookWhatsAppService;

    public WebhooksWhatsAppController(IWebhookWhatsAppService webhookWhatsAppService)
    {
        _webhookWhatsAppService = webhookWhatsAppService;
    }

    [AllowAnonymous]
    [HttpPost("evolution/messages-upsert")]
    public async Task<IActionResult> MensagemRecebida(
        [FromBody] JsonElement payload,
        CancellationToken cancellationToken)
    {
        await _webhookWhatsAppService.ProcessarMensagemRecebidaAsync(payload, cancellationToken);
        return Ok(ApiSuccessResponse.From("Webhook WhatsApp processado."));
    }

    [AllowAnonymous]
    [HttpPost("evolution/send-message")]
    public async Task<IActionResult> MensagemEnviada(
        [FromBody] JsonElement payload,
        CancellationToken cancellationToken)
    {
        await _webhookWhatsAppService.ProcessarMensagemEnviadaAsync(payload, cancellationToken);
        return Ok(ApiSuccessResponse.From("Webhook WhatsApp processado."));
    }
}
