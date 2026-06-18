using System.Security.Cryptography.X509Certificates;
using GLOWAPI.Application.Options;
using Microsoft.AspNetCore.Server.Kestrel.Https;

namespace GLOWAPI.API.Configuration;

public static class KestrelMtlsConfiguration
{
    public static void ConfigurarKestrel(WebApplicationBuilder builder)
    {
        var mtls = MtlsOptionsResolver.Resolver(builder.Configuration);
        if (!mtls.Enabled)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(mtls.ServerCertificatePath)
            || string.IsNullOrWhiteSpace(mtls.ServerCertificateKeyPath))
        {
            throw new InvalidOperationException(
                "Mtls habilitado mas ServerCertificatePath ou ServerCertificateKeyPath ausentes.");
        }

        var serverCert = X509Certificate2.CreateFromPemFile(
            mtls.ServerCertificatePath,
            mtls.ServerCertificateKeyPath);

        var thumbprints = new HashSet<string>(
            mtls.AllowedClientThumbprints
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.Replace(":", string.Empty, StringComparison.OrdinalIgnoreCase)),
            StringComparer.OrdinalIgnoreCase);

        builder.WebHost.ConfigureKestrel(options =>
        {
            options.ListenAnyIP(mtls.PublicPort);

            options.ListenAnyIP(mtls.MutualTlsPort, listenOptions =>
            {
                listenOptions.UseHttps(httpsOptions =>
                {
                    httpsOptions.ServerCertificate = serverCert;
                    httpsOptions.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
                    httpsOptions.ClientCertificateValidation = (certificate, _, _) =>
                    {
                        if (certificate is null)
                        {
                            return false;
                        }

                        if (thumbprints.Count == 0)
                        {
                            return true;
                        }

                        return thumbprints.Contains(certificate.Thumbprint);
                    };
                });
            });
        });
    }
}
