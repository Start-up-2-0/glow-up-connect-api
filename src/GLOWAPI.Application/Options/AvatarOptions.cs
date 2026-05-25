namespace GLOWAPI.Application.Options;

public class AvatarOptions
{
    public const string SectionName = "Avatar";

    public int MaxSizeBytes { get; set; } = 5_242_880;
    public string[] TiposPermitidos { get; set; } = ["image/jpeg", "image/png", "image/webp"];
}
