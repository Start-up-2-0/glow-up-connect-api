namespace GLOWAPI.Application.Helpers;

public static class MercadoPagoWebhookIpn
{
    public static bool EhSemAssinatura(
        string? signatureHeader,
        string? userAgent,
        bool temIdentificadorQuery)
    {
        if (!string.IsNullOrWhiteSpace(signatureHeader))
        {
            return false;
        }

        if (temIdentificadorQuery)
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(userAgent)
            && userAgent.Contains("MercadoPago", StringComparison.OrdinalIgnoreCase);
    }
}
