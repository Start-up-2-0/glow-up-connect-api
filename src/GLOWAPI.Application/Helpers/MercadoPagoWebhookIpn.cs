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

        return PodeProcessarViaConsultaGateway(userAgent, temIdentificadorQuery);
    }

    /// <summary>
    /// IPN/Feed do Mercado Pago: autentica consultando o pagamento/ordem na API
    /// com o Access Token, mesmo se o HMAC falhar ou vier <c>x-signature</c> inválida.
    /// </summary>
    public static bool PodeProcessarViaConsultaGateway(
        string? userAgent,
        bool temIdentificadorQuery)
    {
        if (temIdentificadorQuery)
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(userAgent)
            && userAgent.Contains("MercadoPago", StringComparison.OrdinalIgnoreCase);
    }

    public static string? ExtrairTimestampAssinatura(string? signatureHeader)
    {
        if (string.IsNullOrWhiteSpace(signatureHeader))
        {
            return null;
        }

        foreach (var parte in signatureHeader.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = parte.Split('=', 2, StringSplitOptions.TrimEntries);
            if (kv.Length == 2 && kv[0].Equals("ts", StringComparison.OrdinalIgnoreCase))
            {
                return kv[1];
            }
        }

        return null;
    }
}
