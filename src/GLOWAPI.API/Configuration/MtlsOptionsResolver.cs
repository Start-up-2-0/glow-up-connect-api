using GLOWAPI.Application.Options;

namespace GLOWAPI.API.Configuration;

public static class MtlsOptionsResolver
{
    public static MtlsOptions Resolver(IConfiguration configuration)
    {
        var options = configuration.GetSection(MtlsOptions.SectionName).Get<MtlsOptions>() ?? new MtlsOptions();

        var certPath = FirstNonEmpty(
            configuration[$"{MtlsOptions.SectionName}:ServerCertificatePath"],
            configuration["Mtls__ServerCertificatePath"],
            configuration["MTLS_SERVER_CERT_PATH"]);

        var keyPath = FirstNonEmpty(
            configuration[$"{MtlsOptions.SectionName}:ServerCertificateKeyPath"],
            configuration["Mtls__ServerCertificateKeyPath"],
            configuration["MTLS_SERVER_KEY_PATH"]);

        if (!string.IsNullOrWhiteSpace(certPath))
        {
            options.ServerCertificatePath = certPath;
        }

        if (!string.IsNullOrWhiteSpace(keyPath))
        {
            options.ServerCertificateKeyPath = keyPath;
        }

        var thumbprint = FirstNonEmpty(
            configuration["MTLS_CLIENT_CERT_THUMBPRINT"],
            configuration[$"{MtlsOptions.SectionName}:AllowedClientThumbprints:0"]);

        if (!string.IsNullOrWhiteSpace(thumbprint))
        {
            options.AllowedClientThumbprints = [thumbprint];
        }

        var certsPresent = !string.IsNullOrWhiteSpace(options.ServerCertificatePath)
            && !string.IsNullOrWhiteSpace(options.ServerCertificateKeyPath)
            && File.Exists(options.ServerCertificatePath)
            && File.Exists(options.ServerCertificateKeyPath);

        if (certsPresent)
        {
            options.Enabled = true;
        }
        else if (options.Enabled)
        {
            options.Enabled = false;
        }

        var portValue = configuration["PORT"];
        if (int.TryParse(portValue, out var railwayPort) && railwayPort > 0)
        {
            options.PublicPort = railwayPort;
        }

        var mutualTlsPort = FirstNonEmpty(
            configuration["MTLS_MUTUAL_TLS_PORT"],
            configuration[$"{MtlsOptions.SectionName}:MutualTlsPort"],
            configuration["Mtls__MutualTlsPort"]);

        if (int.TryParse(mutualTlsPort, out var parsedMutualTlsPort) && parsedMutualTlsPort > 0)
        {
            options.MutualTlsPort = parsedMutualTlsPort;
        }

        return options;
    }

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
}
