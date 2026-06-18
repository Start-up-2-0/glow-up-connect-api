namespace GLOWAPI.Application.Options;

public class RateLimitOptions
{
    public const string SectionName = "RateLimit";

    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Requisições permitidas por IP na janela geral (tráfego típico de SPA).
    /// </summary>
    public int BurstMaxRequests { get; set; } = 80;

    public int BurstWindowSeconds { get; set; } = 60;

    /// <summary>
    /// Segundos sugeridos em Retry-After quando exceder o burst sem caracterizar abuso.
    /// </summary>
    public int BurstPenaltySeconds { get; set; } = 30;

    /// <summary>
    /// Multiplicador sobre BurstMaxRequests para bloqueio duro (24h) em tráfego geral.
    /// </summary>
    public int HardBlockMultiplier { get; set; } = 5;

    public int BlockDurationHours { get; set; } = 24;

    /// <summary>
    /// Requisições autenticadas (header de token presente) não contam no burst global.
    /// </summary>
    public bool ExemptAuthenticatedRequests { get; set; } = true;

    /// <summary>
    /// Limite para rotas sensíveis (login, cadastro, recuperação de senha).
    /// </summary>
    public int SensitiveMaxRequests { get; set; } = 15;

    public int SensitiveWindowSeconds { get; set; } = 900;

    public int SensitivePenaltySeconds { get; set; } = 60;

    /// <summary>
    /// Multiplicador sobre SensitiveMaxRequests para bloqueio duro em rotas sensíveis.
    /// </summary>
    public int SensitiveHardBlockMultiplier { get; set; } = 4;

    public string[] SensitivePathPrefixes { get; set; } =
    [
        "/api/auth/login",
        "/api/auth/refresh",
        "/api/auth/forgot-password",
        "/api/auth/reset-password",
        "/api/usuarios"
    ];
}
