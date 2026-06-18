namespace GLOWAPI.Application.Options;

public class RateLimitOptions
{
    public const string SectionName = "RateLimit";

    public bool Enabled { get; set; } = true;

    public int BurstMaxRequests { get; set; } = 3;

    public int BurstWindowSeconds { get; set; } = 5;

    public int BlockDurationHours { get; set; } = 24;
}
