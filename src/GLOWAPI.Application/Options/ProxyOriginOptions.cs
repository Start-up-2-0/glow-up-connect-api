namespace GLOWAPI.Application.Options;

public class ProxyOriginOptions
{
    public const string SectionName = "ProxyOrigin";
    public const string SecretHeaderName = "X-Glow-Proxy-Secret";

    public bool Enabled { get; set; }

    public string Secret { get; set; } = string.Empty;

    public string WebhookPathPrefix { get; set; } = "/api/webhooks";

    public string HealthPath { get; set; } = "/health";
}
