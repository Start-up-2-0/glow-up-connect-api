namespace GLOWAPI.Application.Options;

public class MercadoPagoOptions
{
    public const string SectionName = "MercadoPago";

    /// <summary>
    /// Quando true, cobranças usam Checkout Pro (POST /checkout/preferences) em vez de Checkout Transparente.
    /// </summary>
    public bool UsarCheckoutPro { get; set; }

    public string AccessToken { get; set; } = string.Empty;
    public string ApiBaseUrl { get; set; } = "https://api.mercadopago.com";
    public string NotificationUrl { get; set; } = string.Empty;
    public string SuccessUrl { get; set; } = string.Empty;
    public string FailureUrl { get; set; } = string.Empty;
    public string PendingUrl { get; set; } = string.Empty;

    /// <summary>
    /// URL publica da API (ex.: https://api-staging.up.railway.app). Usada para derivar NotificationUrl no Checkout Pro.
    /// Se vazio, tenta RAILWAY_PUBLIC_DOMAIN.
    /// </summary>
    public string PublicBaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Quando preenchido (ex.: homolog), substitui o e-mail do usuario logado nas chamadas ao Mercado Pago.
    /// Aceita e-mail completo ou identificador de conta de teste (TESTUSER123 ou test_user_123).
    /// </summary>
    public string PayerEmailOverride { get; set; } = string.Empty;

    /// <summary>
    /// ID de plano preapproval existente no Mercado Pago. Quando preenchido, trial reutiliza o plano
    /// em vez de criar um novo via POST /preapproval_plan.
    /// </summary>
    public string PreapprovalPlanId { get; set; } = string.Empty;

    /// <summary>
    /// Quando true, o trial inicia sem criar assinatura recorrente no Mercado Pago.
    /// Util em homolog quando o sandbox de subscriptions retorna 503.
    /// </summary>
    public bool PermitirTrialSemRecorrenciaNoGateway { get; set; }
}
