using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Application.DTOs.Pagamentos;

public class RegistrarWebhookPagamentoRequestDto
{
    public GatewayPagamento Gateway { get; set; }
    public string EventId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
}
