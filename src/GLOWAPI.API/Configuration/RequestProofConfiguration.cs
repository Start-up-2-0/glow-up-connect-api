using GLOWAPI.Application.Options;
using Microsoft.Extensions.Options;

namespace GLOWAPI.API.Configuration;

public static class RequestProofConfiguration
{
    public static void ConfigurarRequestProof(WebApplicationBuilder builder)
    {
        builder.Services.Configure<RequestProofOptions>(options =>
        {
            builder.Configuration.GetSection(RequestProofOptions.SectionName).Bind(options);

            var secret = builder.Configuration["REQUEST_PROOF_SECRET"];
            if (!string.IsNullOrWhiteSpace(secret))
            {
                options.Secret = secret;
            }

            if (string.IsNullOrWhiteSpace(options.Secret))
            {
                options.Enabled = false;
                return;
            }

            var isHosted = builder.Environment.IsStaging() || builder.Environment.IsProduction();
            var configuredEnabled = builder.Configuration.GetValue<bool?>($"{RequestProofOptions.SectionName}:Enabled");
            options.Enabled = configuredEnabled ?? isHosted;
        });
    }
}
