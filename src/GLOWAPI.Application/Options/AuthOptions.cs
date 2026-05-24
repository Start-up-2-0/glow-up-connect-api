namespace GLOWAPI.Application.Options;

public class AuthOptions
{
    public const string SectionName = "Auth";

    public int MaxLoginAttempts { get; set; } = 5;
    public int LockoutMinutes { get; set; } = 15;
    public int SessionMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 7;
    public int SlidingRenewalMinutes { get; set; } = 30;
    public bool ValidateIpOnToken { get; set; }
    public bool ValidateUserAgentOnToken { get; set; }
    public string TokenSalt { get; set; } = string.Empty;
    public string TokenHeaderName { get; set; } = "x-glow-token";
}
