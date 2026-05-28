namespace GLOWAPI.Application.Options;

public class MercadoPagoOptions
{
    public const string SectionName = "MercadoPago";

    public string AccessToken { get; set; } = string.Empty;
    public string ApiBaseUrl { get; set; } = "https://api.mercadopago.com";
    public string NotificationUrl { get; set; } = string.Empty;
    public string SuccessUrl { get; set; } = string.Empty;
    public string FailureUrl { get; set; } = string.Empty;
    public string PendingUrl { get; set; } = string.Empty;
}
