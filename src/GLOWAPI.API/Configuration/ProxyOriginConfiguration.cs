using GLOWAPI.Application.Options;
using Microsoft.Extensions.Configuration;
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
            builder.Configuration.GetSection(MtlsOptions.SectionName).Bind(options);

            var thumbprint = builder.Configuration["MTLS_CLIENT_CERT_THUMBPRINT"];
            if (!string.IsNullOrWhiteSpace(thumbprint))
            {
                options.AllowedClientThumbprints = [thumbprint];
            }

            var certPath = builder.Configuration["MTLS_SERVER_CERT_PATH"];
            var keyPath = builder.Configuration["MTLS_SERVER_KEY_PATH"];
            if (!string.IsNullOrWhiteSpace(certPath))
            {
                options.ServerCertificatePath = certPath;
            }

            if (!string.IsNullOrWhiteSpace(keyPath))
            {
                options.ServerCertificateKeyPath = keyPath;
            }

            if (!string.IsNullOrWhiteSpace(options.ServerCertificatePath)
                && !string.IsNullOrWhiteSpace(options.ServerCertificateKeyPath)
                && File.Exists(options.ServerCertificatePath)
                && File.Exists(options.ServerCertificateKeyPath))
            {
                options.Enabled = true;
            }

            var portValue = builder.Configuration["PORT"];
            if (int.TryParse(portValue, out var railwayPort) && railwayPort > 0)
            {
                options.PublicPort = railwayPort;
            }
        });
    }
}
