using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Domain.Entities;

public class WebhookPagamento
{
    public int Id { get; set; }
    public GatewayPagamento Gateway { get; set; }
    public string EventId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public bool Processado { get; set; }
    public DateTime? ProcessadoEm { get; set; }
    public string ErroProcessamento { get; set; } = string.Empty;
    public DateTime CreateAd { get; set; } = DateTime.UtcNow;
}
