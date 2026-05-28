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

        if (string.IsNullOrWhiteSpace(configuration["POSTGSL"]))
        {
            faltando.Add("POSTGSL");
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

        if (faltando.Count > 0)
        {
            throw new InvalidOperationException(
                $"Variaveis obrigatorias ausentes para ambiente {environmentName}: {string.Join(", ", faltando)}.");
        }
    }
}
