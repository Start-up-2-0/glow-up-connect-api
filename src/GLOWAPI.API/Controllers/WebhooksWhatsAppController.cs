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
    [HttpPost("evolution")]
    public async Task<IActionResult> Evolution(
        [FromBody] JsonElement payload,
        CancellationToken cancellationToken)
    {
        await _webhookWhatsAppService.ProcessarEvolutionWebhookAsync(payload, cancellationToken);
        return Ok(ApiSuccessResponse.From("Webhook WhatsApp processado."));
    }
}
