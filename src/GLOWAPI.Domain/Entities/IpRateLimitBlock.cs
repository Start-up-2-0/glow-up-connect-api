namespace GLOWAPI.Domain.Entities;

public class IpRateLimitBlock
{
    public int Id { get; set; }
    public string Ip { get; set; } = string.Empty;
    public DateTime BlockedUntil { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
