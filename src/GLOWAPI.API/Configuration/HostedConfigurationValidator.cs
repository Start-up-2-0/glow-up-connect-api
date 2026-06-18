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

        var tokenSalt = configuration[$"{AuthOptions.SectionName}:TokenSalt"];
        if (string.IsNullOrWhiteSpace(tokenSalt) || tokenSalt.Length < 32)
        {
            faltando.Add("Auth__TokenSalt (minimo 32 caracteres)");
        }

        var corsOrigins = configuration.GetSection($"{CorsOptions.SectionName}:AllowedOrigins").Get<string[]>() ?? [];
        if (corsOrigins.Length == 0
            || corsOrigins.All(o => o.Contains("localhost", StringComparison.OrdinalIgnoreCase)))
        {
            faltando.Add("Cors__AllowedOrigins (pelo menos uma origem nao-localhost)");
        }

        var mercadoPagoAccessToken = configuration[$"{MercadoPagoOptions.SectionName}:AccessToken"];
        if (string.IsNullOrWhiteSpace(mercadoPagoAccessToken))
        {
            faltando.Add("MercadoPago__AccessToken");
        }
        else if (string.IsNullOrWhiteSpace(configuration[$"{MercadoPagoOptions.SectionName}:WebhookSecret"]))
        {
            faltando.Add("MercadoPago__WebhookSecret");
        }

        var usarCheckoutPro = configuration.GetValue<bool>($"{MercadoPagoOptions.SectionName}:UsarCheckoutPro");
        if (usarCheckoutPro && string.IsNullOrWhiteSpace(frontendBaseUrl))
        {
            faltando.Add("Auth__FrontendBaseUrl (obrigatorio com MercadoPago__UsarCheckoutPro para back_urls)");
        }

        if (string.Equals(environmentName, "Staging", StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(configuration[$"{SwaggerOptions.SectionName}:AccessKey"]))
        {
            faltando.Add("Swagger__AccessKey");
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

        if (string.IsNullOrWhiteSpace(configuration["GLOW_PROXY_SECRET"]))
        {
            faltando.Add("GLOW_PROXY_SECRET");
        }

        var captchaEnabled = configuration.GetValue($"{CaptchaOptions.SectionName}:Enabled", true);
        if (captchaEnabled && string.IsNullOrWhiteSpace(configuration[$"{CaptchaOptions.SectionName}:SecretKey"]))
        {
            faltando.Add("Captcha__SecretKey");
        }

        if (faltando.Count > 0)
        {
            throw new InvalidOperationException(
                $"Variaveis obrigatorias ausentes para ambiente {environmentName}: {string.Join(", ", faltando)}.");
        }
    }
}
