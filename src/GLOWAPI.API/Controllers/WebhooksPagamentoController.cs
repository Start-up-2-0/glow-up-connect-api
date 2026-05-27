using GLOWAPI.API.Models;
using GLOWAPI.Application.DTOs.Pagamentos;
using GLOWAPI.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GLOWAPI.API.Controllers;

[ApiController]
[Route("api/webhooks/pagamentos")]
public class WebhooksPagamentoController : ControllerBase
{
    private readonly IWebhookPagamentoService _webhookPagamentoService;

    public WebhooksPagamentoController(IWebhookPagamentoService webhookPagamentoService)
    {
        _webhookPagamentoService = webhookPagamentoService;
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Registrar(
        [FromBody] RegistrarWebhookPagamentoRequestDto request,
        CancellationToken cancellationToken)
    {
        var webhook = await _webhookPagamentoService.RegistrarAsync(request, cancellationToken);
        return Ok(ApiSuccessResponse<WebhookPagamentoResponseDto>.From(
            webhook.Duplicado
                ? "Webhook de pagamento ja registrado."
                : "Webhook de pagamento registrado com sucesso.",
            webhook));
    }
}
