namespace GLOWAPI.Application.Options;

public class MtlsOptions
{
    public const string SectionName = "Mtls";

    public bool Enabled { get; set; }

    public int PublicPort { get; set; } = 8080;

    public int MutualTlsPort { get; set; } = 8443;

    public string ServerCertificatePath { get; set; } = string.Empty;

    public string ServerCertificateKeyPath { get; set; } = string.Empty;

    public string[] AllowedClientThumbprints { get; set; } = [];
}
