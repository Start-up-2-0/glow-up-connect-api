namespace GLOWAPI.Application.Options;

public class RequestProofOptions
{
    public const string SectionName = "RequestProof";
    public const string DefaultHeaderName = "X-Glow-Request-Proof";
    public const string DefaultContextCookieName = "guc_rqctx";
    public const string DefaultBootstrapPath = "/api/security/request-proof";
    public const string Wildcard = "*";

    public bool Enabled { get; set; }

    public string Secret { get; set; } = string.Empty;

    public int TtlSeconds { get; set; } = 60;

    public int ClockSkewSeconds { get; set; } = 30;

    public string HeaderName { get; set; } = DefaultHeaderName;

    public string ContextCookieName { get; set; } = DefaultContextCookieName;

    public string BootstrapPath { get; set; } = DefaultBootstrapPath;

    public int DefaultPoolSize { get; set; } = 5;

    public int ContextCookieHours { get; set; } = 24;
}
