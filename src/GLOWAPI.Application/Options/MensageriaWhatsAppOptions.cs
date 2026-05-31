namespace GLOWAPI.Application.Options;

public class MensageriaWhatsAppOptions
{
    public const string SectionName = "Mensageria:WhatsApp";

    public string ApiUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string InstanceName { get; set; } = string.Empty;
    public string NumeroPlataforma { get; set; } = string.Empty;
    public string WebhookApiKey { get; set; } = string.Empty;
    public bool Habilitado { get; set; }
}
