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

    /// <summary>
    /// Evolution API v2 usa <c>text</c> na raiz. v1.7.x exige <c>textMessage.text</c>.
    /// </summary>
    public bool UsarApiV2 { get; set; }
}
