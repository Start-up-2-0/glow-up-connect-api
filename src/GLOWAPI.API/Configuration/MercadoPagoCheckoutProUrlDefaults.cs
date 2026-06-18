using GLOWAPI.Application.Options;

namespace GLOWAPI.API.Configuration;

public static class MercadoPagoCheckoutProUrlDefaults
{
    public static void Aplicar(MercadoPagoOptions options, IConfiguration configuration)
    {
        if (!options.UsarCheckoutPro)
        {
            return;
        }

        var frontendBaseUrl = configuration[$"{AuthOptions.SectionName}:FrontendBaseUrl"]?.Trim().TrimEnd('/');
        if (!string.IsNullOrWhiteSpace(frontendBaseUrl))
        {
            options.SuccessUrl = PreencherSeVazio(options.SuccessUrl, $"{frontendBaseUrl}/assinatura/sucesso");
            options.PendingUrl = PreencherSeVazio(options.PendingUrl, $"{frontendBaseUrl}/assinatura/pendente");
            options.FailureUrl = PreencherSeVazio(options.FailureUrl, $"{frontendBaseUrl}/assinatura/falha");
        }

        if (!string.IsNullOrWhiteSpace(options.NotificationUrl))
        {
            return;
        }

        var apiBaseUrl = ResolverApiBaseUrl(configuration);
        if (!string.IsNullOrWhiteSpace(apiBaseUrl))
        {
            options.NotificationUrl = $"{apiBaseUrl}/api/webhooks/pagamentos/mercado-pago";
        }
    }

    private static string? ResolverApiBaseUrl(IConfiguration configuration)
    {
        var configurado = configuration[$"{MercadoPagoOptions.SectionName}:PublicBaseUrl"]?.Trim().TrimEnd('/');
        if (!string.IsNullOrWhiteSpace(configurado))
        {
            return configurado;
        }

        var railwayDomain = configuration["RAILWAY_PUBLIC_DOMAIN"]?.Trim();
        if (!string.IsNullOrWhiteSpace(railwayDomain))
        {
            return $"https://{railwayDomain.TrimEnd('/')}";
        }

        return null;
    }

    private static string PreencherSeVazio(string atual, string fallback) =>
        string.IsNullOrWhiteSpace(atual) ? fallback : atual.Trim();
}
