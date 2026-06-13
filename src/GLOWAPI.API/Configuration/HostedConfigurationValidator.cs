using GLOWAPI.Application.Options;

namespace GLOWAPI.API.Configuration;

public static class HostedConfigurationValidator
{
    public static void ValidarSeAmbienteHospedado(IConfiguration configuration, string environmentName)
    {
        var isHosted = string.Equals(environmentName, "Staging", StringComparison.OrdinalIgnoreCase)
            || string.Equals(environmentName, "Production", StringComparison.OrdinalIgnoreCase);

        if (!isHosted)
        {
            return;
        }

        var faltando = new List<string>();

        if (string.IsNullOrWhiteSpace(configuration["MYSQL_CS"]))
        {
            faltando.Add("MYSQL_CS");
        }

        if (string.IsNullOrWhiteSpace(configuration["RESEND_APITOKEN"]))
        {
            faltando.Add("RESEND_APITOKEN");
        }

        var emailFrom = configuration[$"{MensageriaEmailOptions.SectionName}:From"];
        if (string.IsNullOrWhiteSpace(emailFrom))
        {
            faltando.Add("Mensageria__Email__From");
        }

        var frontendBaseUrl = configuration[$"{AuthOptions.SectionName}:FrontendBaseUrl"];
        if (string.IsNullOrWhiteSpace(frontendBaseUrl))
        {
            faltando.Add("Auth__FrontendBaseUrl");
        }

        var mercadoPagoAccessToken = configuration[$"{MercadoPagoOptions.SectionName}:AccessToken"];
        if (string.IsNullOrWhiteSpace(mercadoPagoAccessToken))
        {
            faltando.Add("MercadoPago__AccessToken");
        }

        var usarCheckoutPro = configuration.GetValue<bool>($"{MercadoPagoOptions.SectionName}:UsarCheckoutPro");
        if (usarCheckoutPro && string.IsNullOrWhiteSpace(frontendBaseUrl))
        {
            faltando.Add("Auth__FrontendBaseUrl (obrigatorio com MercadoPago__UsarCheckoutPro para back_urls)");
        }

        var whatsAppHabilitado = configuration.GetValue<bool>($"{MensageriaWhatsAppOptions.SectionName}:Habilitado");
        if (whatsAppHabilitado)
        {
            if (string.IsNullOrWhiteSpace(configuration[$"{MensageriaWhatsAppOptions.SectionName}:ApiUrl"]))
            {
                faltando.Add("Mensageria__WhatsApp__ApiUrl");
            }

            if (string.IsNullOrWhiteSpace(configuration[$"{MensageriaWhatsAppOptions.SectionName}:ApiKey"]))
            {
                faltando.Add("Mensageria__WhatsApp__ApiKey");
            }

            if (string.IsNullOrWhiteSpace(configuration[$"{MensageriaWhatsAppOptions.SectionName}:InstanceName"]))
            {
                faltando.Add("Mensageria__WhatsApp__InstanceName");
            }
        }

        if (faltando.Count > 0)
        {
            throw new InvalidOperationException(
                $"Variaveis obrigatorias ausentes para ambiente {environmentName}: {string.Join(", ", faltando)}.");
        }
    }
}
