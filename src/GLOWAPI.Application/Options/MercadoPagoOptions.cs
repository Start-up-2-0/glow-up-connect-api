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

    /// <summary>
    /// Quando preenchido (ex.: homolog), substitui o e-mail do usuario logado nas chamadas ao Mercado Pago.
    /// Aceita e-mail completo ou identificador de conta de teste (TESTUSER123 ou test_user_123).
    /// </summary>
    public string PayerEmailOverride { get; set; } = string.Empty;
}
