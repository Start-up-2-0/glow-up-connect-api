namespace GLOWAPI.Application.Options;

public class AvatarOptions
{
    public const string SectionName = "Avatar";

    public int MaxSizeBytes { get; set; } = 524_288;
    public int PersistenciaMaxLadoPx { get; set; } = 384;
    public int PersistenciaQualidadeJpeg { get; set; } = 72;
    public string[] TiposPermitidos { get; set; } = ["image/jpeg", "image/png", "image/webp"];
}
