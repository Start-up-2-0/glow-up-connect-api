using GLOWAPI.Application.DTOs.Pagamentos;

namespace GLOWAPI.Application.Interfaces.Services;

public interface IWebhookPagamentoService
{
    Task<WebhookPagamentoResponseDto> RegistrarAsync(
        RegistrarWebhookPagamentoRequestDto request,
        CancellationToken cancellationToken = default);
}
