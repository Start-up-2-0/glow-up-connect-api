namespace GLOWAPI.Application.Options;

public class CorsOptions
{
    public const string SectionName = "Cors";

    public static readonly string[] DefaultOrigins =
    [
        "http://localhost:5173",
        "http://localhost:3000",
    ];

    public string[] AllowedOrigins { get; set; } = DefaultOrigins;
}
