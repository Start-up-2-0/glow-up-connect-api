namespace GLOWAPI.Application.Options;

public class CaptchaOptions
{
    public const string SectionName = "Captcha";

    public bool Enabled { get; set; }

    public string Provider { get; set; } = "Recaptcha";

    public string SecretKey { get; set; } = string.Empty;

    public double MinimumScore { get; set; } = 0.5;
}
