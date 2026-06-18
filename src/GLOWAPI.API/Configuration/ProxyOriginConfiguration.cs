using GLOWAPI.Application.Options;
using Microsoft.Extensions.Options;

namespace GLOWAPI.API.Configuration;

public static class ProxyOriginConfiguration
{
    public static void ConfigurarProxyOrigin(WebApplicationBuilder builder)
    {
        builder.Services.Configure<ProxyOriginOptions>(options =>
        {
            builder.Configuration.GetSection(ProxyOriginOptions.SectionName).Bind(options);

            var secret = builder.Configuration["GLOW_PROXY_SECRET"];
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
            options.Enabled = isHosted || builder.Configuration.GetValue<bool>($"{ProxyOriginOptions.SectionName}:Enabled");
        });

        builder.Services.Configure<MtlsOptions>(options =>
        {
            var resolved = MtlsOptionsResolver.Resolver(builder.Configuration);
            options.Enabled = resolved.Enabled;
            options.PublicPort = resolved.PublicPort;
            options.MutualTlsPort = resolved.MutualTlsPort;
            options.ServerCertificatePath = resolved.ServerCertificatePath;
            options.ServerCertificateKeyPath = resolved.ServerCertificateKeyPath;
            options.AllowedClientThumbprints = resolved.AllowedClientThumbprints;
        });
    }
}
