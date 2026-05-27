using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.DTOs.Pagamentos;

public record WebhookPagamentoResponseDto(
    int Id,
    string Gateway,
    string EventId,
    string EventType,
    bool Processado,
    bool Duplicado,
    DateTime CreateAd)
{
    public static WebhookPagamentoResponseDto From(WebhookPagamento webhook, bool duplicado) =>
        new(
            webhook.Id,
            webhook.Gateway.ToString(),
            webhook.EventId,
            webhook.EventType,
            webhook.Processado,
            duplicado,
            webhook.CreateAd);
}
